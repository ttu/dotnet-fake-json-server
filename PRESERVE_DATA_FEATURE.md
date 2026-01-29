## Testing the PreserveDataOnRepublish Feature

### Setup
1. Enable the feature in `appsettings.json`:
```json
"DataStore": {
  "PreserveDataOnRepublish": true
}
```

### Usage Flow
1. **First run**: Server starts, no backup exists
2. **Add data**: Use API to add users, posts, etc.
3. **Republish**: Run `dotnet publish` - data gets backed up automatically
4. **After republish**: Original data is lost, but backup exists
5. **Restore**: Call `POST /admin/restore-backup` to restore previous data

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