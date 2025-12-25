const DB_NAME = "polyclinic_db";
const db = db.getSiblingDB(DB_NAME);

db.keys.insertOne({
  "_id": ObjectId("6883d824bd41316b2dd01944"),
  "login": "admin",
  "password_hash": "$2a$12$plcalF5ccCvU5lLOsXktdOHqFSLVCw6r2aPve2NlYOLFskb.UVqKK",
  "access_rights": {
    "database_access": "full",
    "role": "administrator",
    "specific_permissions": [
      "create_users",
      "modify_users",
      "delete_users",
      "view_all_data",
      "modify_all_data",
      "delete_all_data",
      "run_aggregations",
      "export_data",
      "manage_schedules",
      "issue_certificates"
    ]
  },
  "created_date": new Date("2025-08-01T00:00:00.000Z"),
  "modified_date": new Date("2025-11-27T04:33:22.329Z"),
  "last_password_change": new Date("2025-09-03T08:41:13.545Z"),
  "password_expires": null,
  "account_locked": false,
  "failed_login_attempts": 0,
  "last_failed_attempt": null
});

db.keys.insertOne({
  "_id": ObjectId("6883d824bd41316b2dd01945"),
  "login": "maria84",
  "password_hash": "$2a$12$9qFNwjBlPgQHmNPURUfgu.fNCB83xoQt3tcT07FjYyq3qCo3mwcre",
  "access_rights": {
    "database_access": "read_write",
    "role": "operator",
    "specific_permissions": [
      "view_all_data",
      "modify_all_data",
      "run_aggregations",
      "manage_schedules",
      "issue_certificates"
    ]
  },
  "created_date": new Date("2025-08-05T00:00:00.000Z"),
  "modified_date": new Date("2025-08-25T22:58:25.004Z"),
  "last_password_change": new Date("2025-08-05T00:00:00.000Z"),
  "password_expires": null,
  "account_locked": false,
  "failed_login_attempts": 0,
  "last_failed_attempt": null
});

db.keys.insertOne({
  "_id": ObjectId("6883d824bd41316b2dd01946"),
  "login": "bohdan5757",
  "password_hash": "$2a$12$f1WDnV0mwugHohNpXRHe.erl5psyIcUlSfVFeiJ90PKsdzIhGA8be",
  "access_rights": {
    "database_access": "read_only",
    "role": "authorized",
    "specific_permissions": [
      "create_users",
      "modify_users",
      "delete_users",
      "view_all_data",
      "modify_all_data",
      "delete_all_data",
      "run_aggregations",
      "export_data",
      "manage_schedules",
      "issue_certificates"
    ]
  },
  "created_date": new Date("2025-08-01T00:00:00.000Z"),
  "modified_date": new Date("2025-11-27T04:33:41.405Z"),
  "last_password_change": new Date("2025-09-03T08:41:13.545Z"),
  "password_expires": null,
  "account_locked": false,
  "failed_login_attempts": 0,
  "last_failed_attempt": null
});

db.users.insertOne({
  "_id": ObjectId("65a1234567890123456789b1"),
  "login": "admin",
  "password_hash": "$2a$12$plcalF5ccCvU5lLOsXktdOHqFSLVCw6r2aPve2NlYOLFskb.UVqKK",
  "role": "administrator",
  "full_name": "Репетовський Владислав Володимирович",
  "email": "repetovskyi@polyclinic.ua",
  "phone": "+380501234567",
  "created_date": new Date("2025-08-02T00:00:00.000Z"),
  "last_login": new Date("2025-11-27T04:33:22.339Z"),
  "is_active": true,
  "access_rights": {
    "view_data": true,
    "edit_data": true,
    "delete_data": true,
    "run_aggregations": true,
    "save_results": true,
    "manage_users": true
  }
});

db.users.insertOne({
  "_id": ObjectId("65a1234567890123456789b2"),
  "login": "maria84",
  "password_hash": "$2a$12$UAxdPbgaJvvDD9EWWJukxOgw9C7AvSVomy88t1/sYRJJpcrBSaRzC",
  "role": "operator",
  "full_name": "Репетовська Марія Богданівна",
  "email": "repetovskaMaria@polyclinic.ua",
  "phone": "+380501234568",
  "created_date": new Date("2025-08-05T00:00:00.000Z"),
  "last_login": new Date("2025-11-14T17:38:54.065Z"),
  "is_active": true,
  "access_rights": {
    "view_data": true,
    "edit_data": true,
    "delete_data": true,
    "run_aggregations": true,
    "save_results": true,
    "manage_users": false
  }
});

db.users.insertOne({
  "_id": ObjectId("65a1234567890123456789b3"),
  "login": "bohdan5757",
  "password_hash": "$2a$12$f1WDnV0mwugHohNpXRHe.erl5psyIcUlSfVFeiJ90PKsdzIhGA8be",
  "role": "authorized",
  "full_name": "Костенюк Богдан Анісімович",
  "email": "bohdan5757@polyclinic.ua",
  "phone": "+380501234569",
  "created_date": new Date("2025-08-09T00:00:00.000Z"),
  "last_login": new Date("2025-11-27T04:33:42.661Z"),
  "is_active": true,
  "access_rights": {
    "view_data": true,
    "edit_data": false,
    "delete_data": false,
    "run_aggregations": true,
    "save_results": true,
    "manage_users": false
  }
});

print("ok - inserted initial users");