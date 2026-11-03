# Аудит Green & Tasty

Актуалізовано: **3 листопада 2026 року**.

## Висновок

Group Pre-Order має frontend, backend та розгорнутий AWS demo.
Код доступний у [публічному репозиторії](https://github.com/Xevon-dev/epam-marathon-2026-green-and-tasty).
За реалізацією покрито функціональність із `homework-backend.md`:
учасники/суми, owner session, консолідація підтверджених кошиків і видалення гостя.
Повну працездатність гостьового сценарію та waiter view ще потрібно підтвердити E2E.

## Перевірені результати

- Backend: **148 passed, 0 failed**.
- Чотири цільові frontend spec-файли: **21 passed, 0 failed**.
- Production build проходить; initial bundle близько 790 kB, вище warning budget 500 kB, нижче error budget 1 MB.
- Останній повний frontend suite до додавання regression-тестів: **209 passed, 46 failed із 255**.
  34 падіння пов’язані з `spyOnProperty` на non-configurable `FileReader.prototype`,
  інші — з legacy fixtures/providers/inputs. Це не зелений повний suite.
- Read-only перевірки 03.11: GitHub і HTTPS frontend — 200, API страв/локацій — 200;
  зображення та меню завантажуються в браузері.
- Owner menu API без токена повертає 401; Group Order без входу перенаправляє на login.
- Owner-only E2E раніше пройдено на demo; підтвердження участі гостя та waiter view відсутнє.

## Відомі обмеження

1. Group submit потребує існуючого базового **Pre-order**. Це умова `homework-backend.md:119`;
   крок включено до [демо](DEMO_SCRIPT.md).
2. Backend не має локального HTTP-хоста; локально запускаються unit-тести, а frontend використовує налаштований API.
3. `GetGroupReservationStatusHandler` не перевіряє identity/guest token у handler;
   повертає статус і кількість учасників за ID групи. Потрібен окремий перегляд доступу.
4. Для неавторизованого owner API відповідь 401 не містить CORS-заголовка:
   прямий запит із браузера показує network error. Це не підтверджує проблему авторизованого сценарію.
5. У guest store залишаються debug-логи, заголовок вкладки — `RestaurantFrontendApp`.
6. Є великі фотографії (до приблизно 11,6 MB), bundle/CSS/CommonJS warnings та legacy lint/test-проблеми.
7. Backend dependency scan під час `dotnet test` повідомляв NU1902/NU1903.
   Оновлення залежностей потребує окремої перевірки сумісності.

## Публічні файли

Перевірено список tracked-файлів: немає `node_modules`, build-каталогів, локальних AWS credentials,
Terraform state чи `.env` із налаштуваннями середовища. `.env.example` — шаблон AQA.
Пошук типових форматів AWS/GitHub-токенів, JWT та private keys у текстових змінах історії
не дав збігів. Це перевірка за шаблонами, а не гарантія відсутності будь-яких секретів.

Внутрішні account/support/billing-відомості прибрано з актуальної документації.
Попередні коміти зберігаються в git-історії; її переписування не виконувалося.
Авторські відомості EPAM та навчальні/AQA-матеріали збережено.

## CI та розгортання

Автоматичні AWS deploy/apply замінено перевірками без deployment;
нічний запуск AQA проти живого середовища прибрано. Наявність workflow не означає,
що він уже успішно виконався на GitHub. Результати вище отримані локально та на demo.

Джерела вимог — [EPAM_REQUIREMENTS.md](EPAM_REQUIREMENTS.md),
актуальні кроки здачі — [EPAM_SUBMISSION.md](EPAM_SUBMISSION.md).
