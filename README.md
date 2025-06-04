# APBD 10

It is a project which was completed during the APBD course in PJAIT. It has REST API structure and connected
to the Microsoft TSQL server. It allows user/admin to manage devices database.

## Settings
To successfully launch the application, it should have contain appsettings.json file with a "DefaultConnection"
connection string. Here's the template for it (parameters can be changed):

```json
  {
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*", 
  "ConnectionStrings": {
    "DefaultConnection" : "Data Source=localhost; Initial Catalog=master; User Id=sa; Password=database_CONNECTION_2025; Trust Server Certificate=true;"
  },
  "Jwt": {
    "Key": "32-characters-long-JWT-secret",
    "Issuer": "issuer-address",
    "Audience": "audience-addreess"
  }
}
```
## Switching projects

Since first 3 tutorials I did a bunch of bad code that I didn't fix. A couple of bad approaches were used, so
I decided to create a cleaner look project, where I am commited to deliver the best quality code. However, with
enough of courage and time I could've fixed it, but switching Git repos saves me a lot of time.

![Thanks GIF](https://media.giphy.com/media/26gsjCZpPolPr3sBy/giphy.gif)

