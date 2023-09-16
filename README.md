# Finance app

## Features
* View accounts (BAS plans) and balances
* View and create verifications
* View ledger entries

You can also:
* Register sale in one form

### Lacking
In order for this to be a functional program:
* A better template system
* More specialized views - for other business events

## Screenshots

[More screenshots](/Screenshots/)

![Alt text](/Screenshots/Verifications.png "Verifications")

### Overview

![Alt text](/Screenshots/Overview.png "Overview")

## Project

It is built with C# and Blazor for Web Assembly, and it uses the MudBlazor component library for UI.

The actual logic is in the backend service, and data is stored in a Sqlite database.

The parts are: 

* Frontend Web App
* Backend with Web API
* SQL Server database (running in a container)

## Development environment

You need to have .NET 6 SDK installed to build this project.

No other dependencies required. Not even Node.

In the terminal, while in Server directory, run the following command:

```
dotnet run
```

### Expose Blobs via public URL 

To publicly expose Blobs via their URLs you have to change Azurite's configuration.

*(This requires Azurite to have been run once for the files to be created)*

Open the file ```WebApi/.data/azurite/__azurite_db_blob__.json```:

Add the ```"publicAccess": "blob"``` key-value in the section shown below:


```json
        {
            "name": "$CONTAINERS_COLLECTION$",
            "data": [
                {
                    "accountName": "devstoreaccount1",
                    "name": "images",
                    "properties": {
                        "etag": "\"0x1C839AE6CDF11F0\"",
                        "lastModified": "2021-05-14T15:08:51.726Z",
                        "leaseStatus": "unlocked",
                        "leaseState": "available",
                        "hasImmutabilityPolicy": false,
                        "hasLegalHold": false,
              --- >  "publicAccess": "blob" <---- 
                    },
                   // Omitted
        },
```

Then, restart Azurite.

### Seed data
By default, the database will be (re)created with initial data being seeded eveytime you (re)start the app. 

This can be turned on/off in the file ```Server/Data/Seed.cs.```
