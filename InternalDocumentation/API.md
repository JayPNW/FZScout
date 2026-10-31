# Resource Hunter API Documentation

## Reporting a bonus

The request to report a bonus is:
```http
    POST https://fangbreaker.zone/api/client/bonuses
    Content-Type: application/json
    X-API-Key: fz_live_...
```

User generates the API key on the site. It always begins with `fz_live_...`.

The JSON body is:

```json
{
    "serverName": "Fangbreaker",
    "zoneName": "The parsed zone name",
    "bonusKey": "item-exp"
}
```

The 200 OK response should look like:
```
{
    "ok": true,
    "data": {
        "updated": true,
        "zoneBonus": {}
    }
}
```

400 is an error for invalid payload
401 is missing/invalid api  key
403 is a banned/timedout account
404 is the zone or server was not found
500 is a server-side failure

Duplicate submissions will return the same thing, and duplicate submissions are harmless. The backend will disregard them unless it's a change.

The API key itself will tie submissions to the user and any timeouts or bans or whatever are respected. Basically a submission through the API is the same as a user trying to login and submit it on the web gui. 


## GET endpoints

The public endpoints you can use to get bonus / zone data are:

```http
GET https://{SERVER}/api/servers/fangbreaker/bonuses/today
GET https://{SERVER}/api/servers/fangbreaker/bonuses/types
GET https://{SERVER}/api/servers/fangbreaker/bonuses/zones
GET https://{SERVER}/api/servers/frostreaver/bonuses/types
GET https://{SERVER}/api/servers/frostreaver/bonuses/zones
```

Server is either `fangbreaker.zone` or `frostreaver.zone`; it doesn't matter which you use in the `{SERVER}` placeholder.
