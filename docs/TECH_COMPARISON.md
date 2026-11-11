# Technology selection and comparisons

| Option | Benefits | Limitations | Decision |
|---|---|---|---|
| Blazor + C# | One-language stack, reusable Razor, showcase FP C# | Interactive server requires live connection | Selected |
| React + TypeScript | Mature web tools and ecosystem | Domain logic no longer centered on course C# | Not selected |
| Avalonia | Cross-platform desktop | Mobile/browser support less direct | Not selected |
| SQLite | Simple portable local database | Poorer concurrency for multi-user production server | Selected for MVP |
| PostgreSQL | Strong concurrent server DB | More setup/deployment effort | Future option |

Neither Blazor nor SQLite is explicitly mandatory in provided handouts.
