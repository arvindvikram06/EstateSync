# India location data

`india-locations.json` contains only Indian states, union territories, and cities.
It was generated from the `country-state-city` npm package version 3.2.1 that was
previously shipped in the EstateSync frontend. Keeping the reduced data in the API
prevents browsers from downloading and parsing the package's worldwide 8.4 MB dataset.

When updating the source data, preserve this JSON shape:

```json
{
  "states": [
    { "code": "TN", "name": "Tamil Nadu", "cities": ["Chennai"] }
  ]
}
```

State codes must be unique two-letter ISO 3166-2 subdivision suffixes. City names are
deduplicated case-insensitively by `LocationService` when the API starts.
