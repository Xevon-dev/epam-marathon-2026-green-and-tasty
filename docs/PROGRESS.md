# Поточний стан проєкту

Оновлено: **3 листопада 2026 року**.

## Реалізовано та опубліковано

- [Публічний репозиторій](https://github.com/Xevon-dev/epam-marathon-2026-green-and-tasty).
- [Frontend через CloudFront HTTPS](https://dd9nzqrfa7inx.cloudfront.net/).
- [API, eu-north-1/develop](https://kfhwimu435.execute-api.eu-north-1.amazonaws.com/develop).
- Owner API та Angular-сторінка Group Order: учасники, суми, статуси, власний кошик, видалення гостя та консолідований submit.
- Гостьовий вхід за запрошенням, серверне редагування кошика та його підтвердження.
- Виправлено передачу Cognito-токена до owner-маршрутів `/guest/groups/.../sessions`.
- Додано завершення loading-state та повідомлення про відсутній базовий pre-order.

## Перевірки

| Перевірка | Результат |
| --- | --- |
| Backend unit-тести | 148 passed, 0 failed |
| Цільові frontend-тести | 21 passed, 0 failed |
| Production build | Проходить із bundle/CSS/CommonJS warnings |
| Останній повний frontend suite | 209 passed, 46 failed із 255; запуск до додавання regression-тестів |
| Read-only перевірка 03.11 | GitHub і frontend — 200; API страв/локацій — 200 |
| Захист owner API | Без токена — 401; Group Order без входу перенаправляє на login |
| Owner-only E2E | Пройдено; консолідований pre-order збережений |
| Guest contribution і waiter view | Ще не підтверджено end-to-end |

Фінальний тест пройдено **зі слів учасника**. Бал у репозиторії не зберігається.
Завершення анкети для диплома ще не підтверджено.

## Перед завершенням здачі

1. Пройти повний [демо-сценарій](DEMO_SCRIPT.md), залишивши хоча б одного гостя з підтвердженим кошиком.
2. Перевірити консолідоване замовлення в інтерфейсі офіціанта.
3. Заповнити анкету: ПІБ англійською, URL застосунку, URL репозиторію та опис фічі з [EPAM_SUBMISSION.md](EPAM_SUBMISSION.md).

На наданому скриншоті Examinator дедлайн — **05.11.2026 до 08:00**;
часовий пояс там не зазначений. Технічну частину доцільно завершити 4 листопада,
як оголошено в субтитрах. Джерела — [EPAM_REQUIREMENTS.md](EPAM_REQUIREMENTS.md).
