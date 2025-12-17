# Inventory Manager

### Configuration needed to initialize the database

* Install postgresql [download](https://www.postgresql.org/download/)
* Create the database 'inventory'
* Configure the connection string in the [appsettings](https://github.com/brunotorres99/RELEXChallenge/blob/main/src/RELEX.InventoryManager.Api/appsettings.json) file
  ```
  "InventorySettings": {
    "Database": {
        "ConnectionString": "Host=localhost;Database=inventory;Username=postgres;Password=admin;SearchPath=inv"
      }
    }
  ```
* The database tables and seeding stored procedure are created by the migrations

### Executing program

* Set the Api project as start up and execute the project
* Use the swagger or Postman to seed the data, this endpint will execute the seed stored procedure the generate data

### Solution design and performance considerations

* Seeding using a stored procedure for faster inserts
* Indexes added for the most common filters
* Use of stream for the bulk operation
* Use of IAsyncEnumerable for faster retrieving of data and uses less memory
* FluentValidator to validate input data

### Example load or response times

Table with 12000000 records

* Seeding 100000 records -> Query returned successfully in 5 secs 801 msec.
* Seeding 1000000 records -> Query returned successfully in 42 secs 490 msec.
* Seeding 10000000 records -> Query returned successfully in 4 min 26 secs.

* Get single record -> most under 10ms
```
  curl --location 'http://localhost:5132/api/Orders/785fe97f-278c-492c-a0b4-6b84bbf8b9cc'
```

* Create single record -> most under 10ms
```
  curl --location 'http://localhost:5132/api/Orders' \
--header 'Content-Type: application/json' \
--data-raw '{
    "locationCode": "Porto-010",
    "productCode": "apples-010",
    "orderDate": "2025-12-17",
    "quantity": 150,
    "submittedBy": "user_123@store.com",
    "submittedAt": "2025-12-17T23:12:55.834442+00:00"
}'
```

* Update single record -> most under 10ms
```
  curl --location --request PUT 'http://localhost:5132/api/Orders/785fe97f-278c-492c-a0b4-6b84bbf8b9cc' \
--header 'Content-Type: application/json' \
--data-raw '{
    "locationCode": "Porto-010",
    "productCode": "apples-010",
    "orderDate": "2025-12-17",
    "quantity": 152,
    "submittedBy": "user_123@store.com",
    "submittedAt": "2025-12-17T23:12:55.834442+00:00"
}'
```

* Search orders page 1140 -> under 1s
```
  curl --location 'http://localhost:5132/api/Orders?LocationCode=&ProductCode=&OrderDateFrom=2025-09-28&OrderDateTo=2025-12-28&Aggregate=false&PageNumber=1140&PageSize=1000' \
--header 'Content-Type: application/json' \
```

* Search orders with stream with 1140331 records -> under 4s
```
  curl --location 'http://localhost:5132/api/Orders/stream?LocationCode=&ProductCode=&OrderDateFrom=2025-09-28&OrderDateTo=2025-12-28' \
--header 'Content-Type: application/json' \
```

* Bulk create/update orders with stream with 28254 records -> under 41s
```
  curl --location 'http://localhost:5132/api/Orders/bulk' \
--header 'Content-Type: application/json' \
--data-raw '[
    {
        "id": "2ac0754d-1857-4e8c-a194-33e58e3a3981",
        "locationCode": "Aveiro-001",
        "productCode": "bananas-002",
        "orderDate": "2025-11-30",
        "quantity": 94,
        "submittedBy": "user_194@store.com",
        "submittedAt": "2025-11-30T14:29:49.323396+00:00"
    },
...
    {
        "id": "cf0f10ac-8e95-4b51-8be0-17962fcbedde",
        "locationCode": "Coimbra-001",
        "productCode": "potatoes-001",
        "orderDate": "2025-11-30",
        "quantity": 86,
        "submittedBy": "user_998986@store.com",
        "submittedAt": "2025-11-30T23:05:47.273997+00:00"
    }
]'
```

