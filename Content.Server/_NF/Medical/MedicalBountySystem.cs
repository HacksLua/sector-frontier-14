
using System.Linq;
using Content.Server._NF.Bank;
using Content.Server._NF.Medical.Components;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.Medical;
using Content.Shared._NF.Medical.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server._NF.Traits.Assorted;
using Content.Server.Hands.Systems;

namespace Content.Server._NF.Medical;

public sealed partial class MedicalBountySystem : EntitySystem
{
    [Dependency] IAdminLogManager _adminLog = default!;
    [Dependency] IRobustRandom _random = default!;
    [Dependency] IPrototypeManager _proto = default!;
    [Dependency] AudioSystem _audio = default!;
    [Dependency] BankSystem _bank = default!;
    [Dependency] BloodstreamSystem _bloodstream = default!;
    [Dependency] DamageableSystem _damageable = default!;
    [Dependency] HandsSystem _hands = default!;
    [Dependency] PopupSystem _popup = default!;
    [Dependency] PowerReceiverSystem _power = default!;
    [Dependency] SharedAppearanceSystem _appearance = default!;
    [Dependency] SharedContainerSystem _container = default!;
    [Dependency] StackSystem _stack = default!;
    [Dependency] TransformSystem _transform = default!;
    [Dependency] UserInterfaceSystem _ui = default!;

    private List<MedicalBountyPrototype> _cachedPrototypes = new();

    public override void Initialize()
    {
        base.Initialize();

        _proto.PrototypesReloaded += OnPrototypesReloaded;

        SubscribeLocalEvent<MedicalBountyComponent, ComponentStartup>(InitializeMedicalBounty);
        SubscribeLocalEvent<MedicalBountyComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<MedicalBountyRedemptionComponent, RedeemMedicalBountyMessage>(RedeemMedicalBounty);
        SubscribeLocalEvent<MedicalBountyRedemptionComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<MedicalBountyRedemptionComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<MedicalBountyRedemptionComponent, AfterActivatableUIOpenEvent>(OnActivateUI);
        SubscribeLocalEvent<MedicalBountyRedemptionComponent, PowerChangedEvent>(OnPowerChanged);

        CacheBountyPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.ByType.ContainsKey(typeof(MedicalBountyPrototype))
            || (args.Removed?.ContainsKey(typeof(MedicalBountyPrototype)) ?? false))
        {
            CacheBountyPrototypes();
        }
    }

    private void CacheBountyPrototypes()
    {
        _cachedPrototypes = _proto.EnumeratePrototypes<MedicalBountyPrototype>().ToList();
    }

    private void InitializeMedicalBounty(EntityUid entity, MedicalBountyComponent component, ComponentStartup args)
    {
        if (component.BountyInitialized)
            return;

        if (component.Bounty == null)
        {
            if (_cachedPrototypes.Count > 0)
                component.Bounty = _random.Pick(_cachedPrototypes);
            else
                return; // Nothing to do, keep bounty at null.
        }

        // Precondition: check entity can fulfill bounty conditions
        if (!TryComp<DamageableComponent>(entity, out var damageable) ||
            !TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return;

        // Apply damage from prototype, keep track of value
        DamageSpecifier damageToApply = new DamageSpecifier();
        var bountyValueAccum = component.Bounty.BaseReward;
        foreach (var (damageType, damageValue) in component.Bounty.DamageSets)
        {
            if (!_proto.TryIndex<DamageTypePrototype>(damageType, out var damageProto))
                continue;

            var randomDamage = _random.Next(damageValue.MinDamage, damageValue.MaxDamage + 1);
            bountyValueAccum += randomDamage * damageValue.ValuePerPoint;
            damageToApply += new DamageSpecifier(damageProto, randomDamage);
        }
        _damageable.TryChangeDamage(entity, damageToApply, true, damageable: damageable);

        // Inject reagents into chemical solution, if any
        foreach (var (reagentType, reagentValue) in component.Bounty.Reagents)
        {
            if (!_proto.HasIndex<ReagentPrototype>(reagentType))
                continue;

            Solution soln = new Solution();
            var reagentQuantity = _random.Next(reagentValue.MinQuantity, reagentValue.MaxQuantity + 1);
            soln.AddReagent(reagentType, reagentQuantity);
            if (_bloodstream.TryAddToChemicals(entity, soln, bloodstream))
                bountyValueAccum += reagentQuantity * reagentValue.ValuePerPoint;
        }

        // Bounty calculation completed, set output state.
        component.MaxBountyValue = bountyValueAccum;
        component.BountyInitialized = true;
    }

    private void RedeemMedicalBounty(EntityUid uid, MedicalBountyRedemptionComponent component, RedeemMedicalBountyMessage ev)
    {
        // Check that the medical redeemer has a valid medical bounty inside
        if (!_container.TryGetContainer(uid, component.BodyContainer, out var container) ||
            container.ContainedEntities.Count <= 0)
        {
            _popup.PopupEntity(Loc.GetString("medical-bounty-redemption-fail-no-items"), uid);
            _audio.PlayPvs(component.DenySound, uid);
            return;
        }

        // Assumption: only one object can be in the MedicalBountyRedemption
        EntityUid bountyUid = container.ContainedEntities[0];

        if (!TryComp<MedicalBountyComponent>(bountyUid, out var medicalBounty) ||
            medicalBounty.Bounty == null ||
            !TryComp<DamageableComponent>(bountyUid, out var damageable))
        {
            _popup.PopupEntity(Loc.GetString("medical-bounty-redemption-fail-no-bounty"), uid);
            _audio.PlayPvs(component.DenySound, uid);
            return;
        }

        // Check that the entity inside is alive.
        var bounty = medicalBounty.Bounty;
        if (damageable.TotalDamage > bounty.MaximumDamageToRedeem)
        {
            _popup.PopupEntity(Loc.GetString("medical-bounty-redemption-fail-too-much-damage"), uid);
            _audio.PlayPvs(component.DenySound, uid);
            return;
        }

        // Calculate amount of reward to pay out.
        var bountyPayout = medicalBounty.MaxBountyValue;
        foreach (var (damageType, damageVal) in damageable.Damage.DamageDict)
        {
            if (bounty.DamageSets.ContainsKey(damageType))
            {
                bountyPayout -= (int)(bounty.DamageSets[damageType].PenaltyPerPoint * damageVal);
            }
            else
            {
                bountyPayout -= (int)(bounty.PenaltyPerOtherPoint * damageVal);
            }
        }

        string successString = "medical-bounty-redemption-success";
        if (TryComp<MedicalBountyBankPaymentComponent>(ev.Actor, out var bankPayment))
        {
            successString = "medical-bounty-redemption-success-to-station";
            // Find the fractions of the whole to pay out.
            _bank.TrySectorDeposit(bankPayment!.Account, bountyPayout, LedgerEntryType.MedicalBountyTax);
            _adminLog.Add(LogType.MedicalBountyRedeemed, LogImpact.Low, $"{ToPrettyString(ev.Actor):actor} redeemed the medical bounty for {ToPrettyString(bountyUid):subject}. Base value: {bountyPayout} (paid to station accounts).");
        }
        else if (bountyPayout > 0)
        {
            var stackUid = _stack.Spawn(bountyPayout, "Credit", Transform(uid).Coordinates);
            if (!_hands.TryPickupAnyHand(ev.Actor, stackUid))
                _transform.SetLocalRotation(stackUid, Angle.Zero); // Orient these to grid north instead of map north

            _adminLog.Add(LogType.MedicalBountyRedeemed, LogImpact.Low, $"{ToPrettyString(ev.Actor):actor} redeemed the medical bounty for {ToPrettyString(bountyUid):subject}. Base value: {bountyPayout}.");
        }
        // Pay tax accounts
        foreach (var (account, taxCoeff) in component.TaxAccounts)
        {
            _bank.TrySectorDeposit(account, (int)(bountyPayout * taxCoeff), LedgerEntryType.MedicalBountyTax);
        }

        QueueDel(bountyUid);

        _popup.PopupEntity(Loc.GetString(successString), uid);
        _audio.PlayPvs(component.RedeemSound, uid);
        UpdateUserInterface(uid, component);
    }

    private void OnEntityInserted(EntityUid uid, MedicalBountyRedemptionComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateUserInterface(uid, component);
        _appearance.SetData(uid, MedicalBountyRedemptionVisuals.Full, true);
    }

    private void OnEntityRemoved(EntityUid uid, MedicalBountyRedemptionComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateUserInterface(uid, component);
        _appearance.SetData(uid, MedicalBountyRedemptionVisuals.Full, false);
    }

    private void OnActivateUI(EntityUid uid, MedicalBountyRedemptionComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, MedicalBountyRedemptionComponent component, PowerChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    public void UpdateUserInterface(EntityUid uid, MedicalBountyRedemptionComponent component)
    {
        if (!_ui.HasUi(uid, MedicalBountyRedemptionUiKey.Key))
            return;

        var actor = _ui.GetActors(uid, MedicalBountyRedemptionUiKey.Key).FirstOrDefault();

        if (!_power.IsPowered(uid))
        {
            _ui.CloseUis(uid);
            return;
        }

        _ui.SetUiState(uid, MedicalBountyRedemptionUiKey.Key, GetUserInterfaceState(uid, component, actor));
    }

    public void OnMobStateChanged(EntityUid uid, MedicalBountyComponent _, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical ||
            args.NewMobState == MobState.Alive)
        {
            RemComp<StinkyTraitComponent>(uid);
        }
    }

    private MedicalBountyRedemptionUIState GetUserInterfaceState(EntityUid uid, MedicalBountyRedemptionComponent component, EntityUid actor)
    {
        var paidToStation = HasComp<MedicalBountyBankPaymentComponent>(actor);
        // Check that the medical redeemer has a valid medical bounty inside
        if (!_container.TryGetContainer(uid, component.BodyContainer, out var container) ||
            container.ContainedEntities.Count <= 0)
        {
            return new MedicalBountyRedemptionUIState(MedicalBountyRedemptionStatus.NoBody, 0, paidToStation);
        }

        // Assumption: only one object can be stored in the MedicalBountyRedemption entity
        EntityUid bountyUid = container.ContainedEntities[0];

        // We either have no value or no way to accurately calculate the value of the bounty.
        if (!TryComp<MedicalBountyComponent>(bountyUid, out var medicalBounty) ||
            medicalBounty.Bounty == null ||
            !TryComp<DamageableComponent>(bountyUid, out var damageable) ||
            !TryComp<MobStateComponent>(bountyUid, out var mobState))
        {
            return new MedicalBountyRedemptionUIState(MedicalBountyRedemptionStatus.NoBounty, 0, paidToStation);
        }

        // Check that the entity inside is sufficiently healed.
        var bounty = medicalBounty.Bounty;
        if (damageable.TotalDamage > bounty.MaximumDamageToRedeem)
        {
            return new MedicalBountyRedemptionUIState(MedicalBountyRedemptionStatus.TooDamaged, 0, paidToStation);
        }

        // Check that the mob is alive.
        if (mobState.CurrentState != Shared.Mobs.MobState.Alive)
        {
            return new MedicalBountyRedemptionUIState(MedicalBountyRedemptionStatus.NotAlive, 0, paidToStation);
        }

        // Bounty is redeemable, calculate amount of reward to pay out.
        var bountyPayout = medicalBounty.MaxBountyValue;
        foreach (var (damageType, damageVal) in damageable.Damage.DamageDict)
        {
            if (bounty.DamageSets.ContainsKey(damageType))
            {
                bountyPayout -= (int)(bounty.DamageSets[damageType].PenaltyPerPoint * damageVal);
            }
            else
            {
                bountyPayout -= (int)(bounty.PenaltyPerOtherPoint * damageVal);
            }
        }

        return new MedicalBountyRedemptionUIState(MedicalBountyRedemptionStatus.Valid, int.Max(bountyPayout, 0), paidToStation);
    }
}
