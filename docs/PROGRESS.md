# Прогрес роботи

Останнє оновлення: 31 жовтня 2026 року

## Виконано

- Переглянуто структуру проєкту, README компонентів, Terraform, ключові API-маршрути та матеріали backend/AQA homework.
- Проаналізовано надані субтитри зустрічей 1–8 і окремий файл субтитрів 9-ї зустрічі.
- Створено `docs/PROJECT_AUDIT.md` та `docs/EPAM_REQUIREMENTS.md`.
- Ініціалізовано локальний Git; зафіксовано початковий стан у коміті `7f245a3`. Remote не налаштований, `git push` не виконувався.
- У `.gitignore` додано виключення для локальних Python-середовищ, AWS/Syndicate-конфігурації, `.env`-варіантів і Terraform plan-файлів.
- Підготовлено сумісні інструменти без заміни глобальних Node/Python: Node.js 24.16.0 у `%LOCALAPPDATA%\Programs\marathon-tools`, .NET SDK 10.0.403 у `%USERPROFILE%\.dotnet`, Python 3.11.9 встановлено поруч із Python 3.13.5.
- `dotnet test .\restaurant-backend-app.sln`: 141 passed, 0 failed, 0 skipped. Є NuGet advisory та compiler warnings, зафіксовані в аудиті.
- Запущено `npm ci` у frontend; результат ще очікується.

## Не виконувалося

- AWS identity/profile check, Terraform init/plan/apply/destroy, Syndicate deploy, AWS resource creation.
- GitHub repo creation, git remote або push.
- Локальний запуск Angular у браузері; frontend unit/build ще не перевірені.
- Реалізація додаткової фічі ще не розпочата.

## Наступні кроки

1. Дочекатися результату `npm ci`; виконати Angular build/unit tests та відкрити локальний frontend.
2. Перевірити frontend/backend контракти для owner flow групового pre-order.
3. Реалізувати узгоджену повноцінну додаткову функцію на frontend і backend з тестами.
4. Підготувати безпечний AWS plan; перед будь-яким створенням ресурсів отримати підтвердження користувача.
5. Завершити сайт, перевірити його та підготувати посилання для анкети до 4 листопада включно.
