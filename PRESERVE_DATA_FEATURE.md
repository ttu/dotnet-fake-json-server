## Testing the PreserveDataOnRepublish Feature

### Setup
1. Enable the feature in `appsettings.json`:
```json
"DataStore": {
  "PreserveDataOnRepublish": true
}
```

### Usage Flow
1. **Server starts**: If backup enabled and no .backup exists, current datastore.json is backed up
2. **Add data**: Use API to add users, posts, etc. during runtime
3. **Republish**: Run `dotnet publish` - creates new build but doesn't create backup itself
4. **New startup**: The new datastore.json will only be backed up on next startup if no .backup exists
5. **Restore**: Call `POST /admin/restore-backup` to restore previous data

**Note**: To ensure newly added runtime data is captured, delete the existing .backup file before restarting after adding data.

### API Endpoints
```bash
# Restore previous data after republish
curl -X POST http://localhost:57602/admin/restore-backup

# Reload data from file
curl -X POST http://localhost:57602/admin/reload
```

### Files Created
- `datastore.json` - Current data file
- `datastore.json.backup` - Backup of previous data (created automatically)