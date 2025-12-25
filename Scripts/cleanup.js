const DB_NAME = "polyclinic_db";
const db = db.getSiblingDB(DB_NAME);

print(`\nConnected to database: ${DB_NAME}`);

print("starting cleanup...\n");

const collectionsToClean = [
    'specialties',
    'rooms',
    'doctors',
    'patients',
    'diagnoses',
    'procedures',
    'schedules',
    'appointments',
    'examinations',
    'certificates',
    'patient_procedures',
    'vaccinations',
    'home_visits',
	'users',
	'keys'
];

let successCount = 0;
let errorCount = 0;
const results = {};

collectionsToClean.forEach(collectionName => {
    try {
        const collection = db.getCollection(collectionName);
        const countBefore = collection.countDocuments();
        
        if (countBefore > 0) {
            const result = collection.deleteMany({});
            results[collectionName] = {
                status: 'success',
                deletedCount: result.deletedCount,
                countBefore: countBefore
            };
            print(`${collectionName.padEnd(25)} : deleted ${result.deletedCount} documents`);
            successCount++;
        } else {
            results[collectionName] = {
                status: 'empty',
                deletedCount: 0,
                countBefore: 0
            };
            print(`○ ${collectionName.padEnd(25)} : already empty`);
            successCount++;
        }
    } catch (e) {
        results[collectionName] = {
            status: 'error',
            error: e.message
        };
        print(`${collectionName.padEnd(25)} : ERROR - ${e.message}`);
        errorCount++;
    }
});

print("\ndropping indexes...");

collectionsToClean.forEach(collectionName => {
    try {
        const collection = db.getCollection(collectionName);
        const indexes = collection.getIndexes();
        
        indexes.forEach(index => {
            if (index.name !== '_id_') {
                try {
                    collection.dropIndex(index.name);
                    print(`  Dropped index: ${collectionName}.${index.name}`);
                } catch (e) {
                    print(`  Failed to drop index: ${collectionName}.${index.name}`);
                }
            }
        });
    } catch (e) {
        print(`  Error processing indexes for ${collectionName}: ${e.message}`);
    }
});



const totalDeleted = Object.values(results).reduce((sum, r) => {
    return sum + (r.deletedCount || 0);
}, 0);

print(`\nTotal collections processed: ${collectionsToClean.length}`);
print(`Successfully cleaned: ${successCount}`);
print(`Errors: ${errorCount}`);
print(`Total documents deleted: ${totalDeleted}`);




