# MiniShop (ADO.NET + PostgreSQL)

Консольное приложение на .NET и ADO.NET (`Npgsql`, `System.Data`) для управления категориями, товарами, движением склада, отчётами и обновлением данных через `DataSet`.

## Как создать базу данных

1. Убедитесь, что сервер PostgreSQL запущен.
2. Создайте пустую базу данных `minishop`:
   ```sql
   CREATE DATABASE minishop;
   ```
3. При необходимости обновите строку подключения в `Data/Database.cs`.

## Как запустить

```bash
dotnet restore
dotnet run
```

При запуске приложение проверяет подключение, создаёт таблицы (если их ещё нет) и добавляет 3 категории и 6 товаров без создания дубликатов при повторном запуске.

## Пункты меню

- **Категории:** список, поиск по id, добавление, обновление, удаление (с обработкой `PostgresException` для дубликатов и товаров внутри категории).
- **Товары:** список, поиск по id, добавление, обновление, удаление (с проверкой полей перед сохранением).
- **Поиск и фильтры:** поиск по части названия без учёта регистра (`ILIKE`) и параметризованный фильтр по категории, минимальной и максимальной цене.
- **Операции склада:** продажа (`RegisterSale`) и поступление (`Restock`) в одной транзакции на одном подключении.
- **Отчёты:**
  1. Products with category
  2. Low stock (товары с остатком ниже порога)
  3. Category summary (количество товаров и стоимость остатка, пустые категории с `0`)
  4. Sales per product (продано единиц по убыванию, товары без продаж с `0`)
- **DataSet:** загрузка товаров через `NpgsqlDataAdapter` и `NpgsqlCommandBuilder`, повышение цен выбранной категории на 10% (округление до 2 знаков), вывод числа изменённых строк (`Modified`), сохранение одним `adapter.Update` и проверка в новом `DataSet`.

## Схема базы данных

```sql
categories (
    id    SERIAL PRIMARY KEY,
    name  VARCHAR(100) NOT NULL UNIQUE
)

products (
    id           SERIAL PRIMARY KEY,
    name         VARCHAR(150) NOT NULL,
    price        NUMERIC(10,2) NOT NULL CHECK (price > 0),
    quantity     INT NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    category_id  INT NOT NULL REFERENCES categories(id)
)

stock_movements (
    id          SERIAL PRIMARY KEY,
    product_id  INT NOT NULL REFERENCES products(id),
    change_qty  INT NOT NULL CHECK (change_qty <> 0),
    moved_at    TIMESTAMP NOT NULL DEFAULT NOW()
)
```
