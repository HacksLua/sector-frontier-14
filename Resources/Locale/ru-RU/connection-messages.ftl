whitelist-not-whitelisted = Вас нет в вайтлисте.
# proper handling for having a min/max or not
whitelist-playercount-invalid =
    { $min ->
        [0] Вайтлист для этого сервера применяется только для числа игроков ниже { $max }.
       *[other]
            Вайтлист для этого сервера применяется только для числа игроков выше { $min } { $max ->
                [2147483647] ->  так что, возможно, вы сможете присоединиться позже.
               *[other] ->  и ниже { $max } игроков, так что, возможно, вы сможете присоединиться позже.
            }
    }
whitelist-not-whitelisted-rp = Вас нет в вайтлисте. Чтобы попасть в вайтлист, посетите наш Discord (ссылку можно найти по адресу https://discord.gg/luaworld).
cmd-whitelistadd-desc = Добавить игрока в вайтлист сервера.
cmd-whitelistadd-help = Использование: whitelistadd <username>
cmd-whitelistadd-existing = { $username } уже находится в вайтлисте!
cmd-whitelistadd-added = { $username } добавлен в вайтлист
cmd-whitelistadd-not-found = Не удалось найти игрока '{ $username }'
cmd-whitelistadd-arg-player = [player]
cmd-whitelistremove-desc = Удалить игрока с вайтлиста сервера.
cmd-whitelistremove-help = Использование: whitelistremove <username>
cmd-whitelistremove-existing = { $username } не находится в вайтлисте!
cmd-whitelistremove-removed = { $username } удалён с вайтлиста
cmd-whitelistremove-not-found = Не удалось найти игрока '{ $username }'
cmd-whitelistremove-arg-player = [player]
cmd-kicknonwhitelisted-desc = Кикнуть всег игроков не в белом списке с сервера.
cmd-kicknonwhitelisted-help = Использование: kicknonwhitelisted
ban-banned-permanent = Этот бан можно только обжаловать. Для этого посетите { $link }.
ban-banned-permanent-appeal = Этот бан можно только обжаловать. Для этого посетите { $link }.
ban-expires = Вы получили бан на { $duration } минут, и он истечёт { $time } по UTC (для московского времени добавьте 3 часа).
ban-banned-1 = Вам, или другому пользователю этого компьютера или соединения, запрещено здесь играть.
ban-banned-2 = Причина бана: "{ $reason }"
ban-banned-3 = Попытки обойти этот бан, например, путём создания нового аккаунта, будут фиксироваться.
soft-player-cap-full = Сервер заполнен!
panic-bunker-account-denied = Этот сервер находится в режиме "Бункер", часто используемом в качестве меры предосторожности против рейдов. Новые подключения от аккаунтов, не соответствующих определённым требованиям, временно не принимаются. Повторите попытку позже.
panic-bunker-account-denied-reason = Этот сервер находится в режиме "Бункер", часто используемом в качестве меры предосторожности против рейдов. Новые подключения от аккаунтов, не соответствующих определённым требованиям, временно не принимаются. Повторите попытку позже. Причина: "{ $reason }"
panic-bunker-account-reason-account = Ваш аккаунт Space Station 14 слишком новый. Он должен быть старше { $minutes } минут.
panic-bunker-account-reason-overall =
    Необходимо минимальное отыгранное Вами время на сервере — { $minutes } { $minutes ->
        [one] минута
        [few] минуты
       *[other] минут
    }.
whitelist-playtime = У вас недостаточно игрового времени, чтобы присоединиться к этому серверу. Вам нужно как минимум { $minutes } минут игрового времени, чтобы присоединиться к этому серверу.
whitelist-player-count = Этот сервер в данный момент не принимает игроков. Пожалуйста, повторите попытку позже.
whitelist-notes = У вас слишком много заметок от администрации, чтобы присоединиться к этому серверу. Вы можете проверить свои заметки, набрав /adminremarks в чате.
whitelist-manual = Вы отсутствуете в белом списке этого сервера.
whitelist-blacklisted = Вы находитесь в чёрном списке этого сервера.
whitelist-always-deny = Вам запрещено присоединяться к этому серверу.
whitelist-fail-prefix = Не внесён в белый список: { $msg }
cmd-blacklistadd-desc = Добавить игрока в чёрный список сервера.
cmd-blacklistadd-help = Использование: blacklistadd <username>
cmd-blacklistadd-existing = { $username } уже находится в чёрном списке!
cmd-blacklistadd-added = { $username } добавлен в чёрный список
cmd-blacklistadd-not-found = Не удалось найти игрока '{ $username }'
cmd-blacklistadd-arg-player = [player]
cmd-blacklistremove-desc = Удалить игрока из чёрного списка сервера.
cmd-blacklistremove-help = Использование: blacklistremove <username>
cmd-blacklistremove-existing = { $username } не находится в чёрном списке!
cmd-blacklistremove-removed = { $username } удалён из чёрного списка
cmd-blacklistremove-not-found = Не удалось найти игрока '{ $username }'
cmd-blacklistremove-arg-player = [player]
generic-misconfigured = Сервер неправильно настроен и не принимает игроков. Пожалуйста, свяжитесь с владельцем сервера и повторите попытку позже.
ipintel-server-ratelimited = На этом сервере используется система безопасности с внешней проверкой, которая достигла своего максимального предела проверки. Пожалуйста, обратитесь за помощью к администрации сервера и повторите попытку позже.
ipintel-unknown = На этом сервере используется система безопасности с внешней проверкой, но она столкнулась с ошибкой. Пожалуйста, обратитесь за помощью к администрации сервера и повторите попытку позже.
ipintel-suspicious = Похоже, вы подключаетесь через центр обработки данных или VPN. По административным причинам мы не разрешаем играть через VPN-соединения. Пожалуйста, обратитесь за помощью к администрации сервера, если вы считаете, что это ошибочно.
hwid-required = Your client has refused to send a hardware id. Please contact the administration team for further assistance.
baby-jail-account-denied = Этот сервер предназначен для новичков и тех, кто хочет им помочь. Новые подключения от слишком старых аккаунтов или аккаунтов, не входящих в белый список, не принимаются. Ознакомьтесь с другими серверами и узнайте всё, что может предложить Space Station 14. Удачи!
baby-jail-account-denied-reason = Этот сервер предназначен для новичков и тех, кто хочет им помочь. Новые подключения от слишком старых аккаунтов или аккаунтов, не входящих в белый список, не принимаются. Ознакомьтесь с другими серверами и узнайте всё, что может предложить Space Station 14. Удачи! Причина: "{ $reason }"
baby-jail-account-reason-account = Ваш аккаунт Space Station 14 слишком старый. Он должен быть моложе { $minutes } минут.
baby-jail-account-reason-overall = Ваше общее время игры на сервере должно быть меньше { $minutes } минут.
