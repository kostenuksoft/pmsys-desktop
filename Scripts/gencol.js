// appointments
db.createCollection("appointments", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'patient_id',
        'doctor_id',
        'appointment_date',
        'appointment_time',
        'type',
        'status',
        'created_date'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        patient_id: {
          bsonType: 'objectId'
        },
        doctor_id: {
          bsonType: 'objectId'
        },
        room_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        appointment_date: {
          bsonType: 'date'
        },
        appointment_time: {
          bsonType: 'string',
          pattern: '^([01]?[0-9]|2[0-3]):[0-5][0-9]$'
        },
        type: {
          'enum': [
            'primary',
            'secondary',
            'checkup',
            'vaccination',
            'procedure'
          ]
        },
        status: {
          'enum': [
            'scheduled',
            'in_progress',
            'completed',
            'cancelled',
            'no_show'
          ]
        },
        complaints: {
          bsonType: [
            'string',
            'null'
          ]
        },
        created_date: {
          bsonType: 'date'
        },
        created_by: {
          bsonType: 'objectId'
        }
      }
    }
  }
});

// certificates
db.createCollection("certificates", {
  validator: {
    $jsonSchema: {
      additionalProperties: false,
      bsonType: 'object',
      required: [
        'certificate_number',
        'type',
        'patient_id',
        'doctor_id',
        'issue_date'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        certificate_number: {
          bsonType: 'string',
          pattern: '^C[0-9]{8}$'
        },
        type: {
          'enum': [
            'sick_leave',
            'health',
            'vaccination',
            'driver',
            'pool',
            'work',
            'education',
            'dispensary'
          ]
        },
        patient_id: {
          bsonType: 'objectId'
        },
        doctor_id: {
          bsonType: 'objectId'
        },
        issue_date: {
          bsonType: 'date'
        },
        valid_from: {
          bsonType: 'date'
        },
        valid_until: {
          bsonType: [
            'date',
            'null'
          ]
        },
        diagnosis_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        content: {
          bsonType: 'string'
        },
        purpose: {
          bsonType: [
            'string',
            'null'
          ]
        },
        created_by: {
          bsonType: 'objectId'
        }
      }
    }
  }
});

// diagnoses
db.createCollection("diagnoses", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'icd_code',
        'name',
        'category'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        icd_code: {
          bsonType: 'string'
        },
        name: {
          bsonType: 'string'
        },
        category: {
          bsonType: 'string'
        },
        description: {
          bsonType: 'string'
        }
      }
    }
  }
});

// doctors
db.createCollection("doctors", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'employee_number',
        'full_name',
        'specialty_id',
        'hire_date',
        'is_active'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        employee_number: {
          bsonType: 'string',
          pattern: '^D[0-9]{5}$'
        },
        full_name: {
          bsonType: 'string'
        },
        birth_date: {
          bsonType: 'date'
        },
        specialty_id: {
          bsonType: 'objectId'
        },
        category: {
          'enum': [
            'highest',
            'first',
            'second',
            'none'
          ]
        },
        experience_years: {
          bsonType: 'int',
          minimum: 0
        },
        hire_date: {
          bsonType: 'date'
        },
        phone: {
          bsonType: 'string'
        },
        email: {
          bsonType: 'string'
        },
        room_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        is_district_doctor: {
          bsonType: 'bool'
        },
        district_area: {
          bsonType: [
            'string',
            'null'
          ]
        },
        is_active: {
          bsonType: 'bool'
        },
        certifications: {
          bsonType: 'array',
          items: {
            bsonType: 'object',
            properties: {
              name: {
                bsonType: 'string'
              },
              issue_date: {
                bsonType: 'date'
              },
              expiry_date: {
                bsonType: 'date'
              }
            }
          }
        }
      }
    }
  }
});

// examinations
db.createCollection("examinations", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'appointment_id',
        'patient_id',
        'doctor_id',
        'examination_date'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        appointment_id: {
          bsonType: 'objectId'
        },
        patient_id: {
          bsonType: 'objectId'
        },
        doctor_id: {
          bsonType: 'objectId'
        },
        examination_date: {
          bsonType: 'date'
        },
        anamnesis: {
          bsonType: 'string'
        },
        objective_status: {
          bsonType: 'string'
        },
        diagnosis_ids: {
          bsonType: 'array',
          items: {
            bsonType: 'objectId'
          }
        },
        recommendations: {
          bsonType: 'string'
        },
        sick_leave_from: {
          bsonType: [
            'date',
            'null'
          ]
        },
        sick_leave_to: {
          bsonType: [
            'date',
            'null'
          ]
        },
        follow_up_date: {
          bsonType: [
            'date',
            'null'
          ]
        }
      }
    }
  }
});

// guest_requests
db.createCollection("guest_requests", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'guest_user_id',
        'request_date',
        'status',
        'message'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        guest_user_id: {
          bsonType: 'objectId'
        },
        request_date: {
          bsonType: 'date'
        },
        status: {
          'enum': [
            'pending',
            'approved',
            'rejected'
          ]
        },
        message: {
          bsonType: 'string'
        },
        admin_response: {
          bsonType: [
            'string',
            'null'
          ]
        },
        processed_date: {
          bsonType: [
            'date',
            'null'
          ]
        },
        processed_by: {
          bsonType: [
            'objectId',
            'null'
          ]
        }
      }
    }
  }
});

// home_visits
db.createCollection("home_visits", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'patient_name',
        'address',
        'phone',
        'call_date',
        'urgency',
        'status',
        'received_by'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        patient_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        patient_name: {
          bsonType: 'string'
        },
        address: {
          bsonType: 'string'
        },
        phone: {
          bsonType: 'string'
        },
        alternative_phone: {
          bsonType: [
            'string',
            'null'
          ]
        },
        call_date: {
          bsonType: 'date'
        },
        call_time: {
          bsonType: 'string'
        },
        urgency: {
          'enum': [
            'regular',
            'urgent',
            'emergency'
          ]
        },
        symptoms: {
          bsonType: 'string'
        },
        assigned_doctor_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        visit_date: {
          bsonType: [
            'date',
            'null'
          ]
        },
        visit_time_slot: {
          bsonType: [
            'string',
            'null'
          ]
        },
        status: {
          'enum': [
            'new',
            'assigned',
            'in_progress',
            'completed',
            'cancelled'
          ]
        },
        status_updated: {
          bsonType: 'date'
        },
        received_by: {
          bsonType: 'objectId'
        },
        notes: {
          bsonType: [
            'string',
            'null'
          ]
        },
        examination_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        }
      }
    }
  }
});

// keys
db.createCollection("keys", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'login',
        'password_hash',
        'access_rights',
        'created_date'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        login: {
          bsonType: 'string',
          minLength: 3,
          maxLength: 50,
          description: 'Unique user login'
        },
        password_hash: {
          bsonType: 'string',
          description: 'Hashed password using bcrypt or similar'
        },
        access_rights: {
          bsonType: 'object',
          required: [
            'database_access',
            'role'
          ],
          properties: {
            database_access: {
              bsonType: 'string',
              'enum': [
                'full',
                'read_write',
                'read_only',
                'none'
              ],
              description: 'Level of database access'
            },
            role: {
              bsonType: 'string',
              'enum': [
                'administrator',
                'operator',
                'authorized',
                'guest'
              ],
              description: 'User role in the system'
            },
            specific_permissions: {
              bsonType: 'array',
              items: {
                bsonType: 'string',
                'enum': [
                  'create_users',
                  'modify_users',
                  'delete_users',
                  'view_all_data',
                  'modify_all_data',
                  'delete_all_data',
                  'run_aggregations',
                  'export_data',
                  'manage_schedules',
                  'issue_certificates'
                ]
              },
              description: 'Specific permissions granted to the user'
            }
          }
        },
        created_date: {
          bsonType: 'date'
        },
        modified_date: {
          bsonType: 'date'
        },
        last_password_change: {
          bsonType: 'date'
        },
        password_expires: {
          bsonType: [
            'date',
            'null'
          ]
        },
        account_locked: {
          bsonType: 'bool',
          description: 'Whether the account is locked'
        },
        failed_login_attempts: {
          bsonType: 'int',
          minimum: 0
        },
        last_failed_attempt: {
          bsonType: [
            'date',
            'null'
          ]
        }
      }
    }
  }
});

// patient_procedures
db.createCollection("patient_procedures", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'patient_id',
        'procedure_id',
        'prescribed_by',
        'prescribed_date',
        'status'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        patient_id: {
          bsonType: 'objectId'
        },
        procedure_id: {
          bsonType: 'objectId'
        },
        prescribed_by: {
          bsonType: 'objectId'
        },
        prescribed_date: {
          bsonType: 'date'
        },
        performed_by: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        performed_date: {
          bsonType: [
            'date',
            'null'
          ]
        },
        room_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        status: {
          'enum': [
            'prescribed',
            'scheduled',
            'completed',
            'cancelled'
          ]
        },
        results: {
          bsonType: [
            'string',
            'null'
          ]
        },
        notes: {
          bsonType: [
            'string',
            'null'
          ]
        }
      }
    }
  }
});

// patients
db.createCollection("patients", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'medical_record_number',
        'full_name',
        'birth_date',
        'registration_date'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        medical_record_number: {
          bsonType: 'string',
          pattern: '^P[0-9]{7}$'
        },
        full_name: {
          bsonType: 'string'
        },
        birth_date: {
          bsonType: 'date'
        },
        gender: {
          'enum': [
            'male',
            'female'
          ]
        },
        address: {
          bsonType: 'object',
          required: [
            'street',
            'building',
            'city',
            'postal_code'
          ],
          properties: {
            street: {
              bsonType: 'string'
            },
            building: {
              bsonType: 'string'
            },
            apartment: {
              bsonType: [
                'string',
                'null'
              ]
            },
            entrance: {
              bsonType: [
                'string',
                'null'
              ]
            },
            floor: {
              bsonType: [
                'int',
                'null'
              ]
            },
            city: {
              bsonType: 'string'
            },
            district: {
              bsonType: 'string'
            },
            postal_code: {
              bsonType: 'string'
            }
          }
        },
        phone: {
          bsonType: 'string'
        },
        alternative_phone: {
          bsonType: [
            'string',
            'null'
          ]
        },
        email: {
          bsonType: [
            'string',
            'null'
          ]
        },
        assigned_doctor_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        health_status: {
          'enum': [
            'healthy',
            'chronic',
            'acute',
            'recovery',
            'observation'
          ]
        },
        blood_type: {
          'enum': [
            'A+',
            'A-',
            'B+',
            'B-',
            'AB+',
            'AB-',
            'O+',
            'O-',
            null
          ]
        },
        allergies: {
          bsonType: 'array',
          items: {
            bsonType: 'string'
          }
        },
        registration_date: {
          bsonType: 'date'
        },
        is_active: {
          bsonType: 'bool'
        }
      }
    }
  }
});

// procedures
db.createCollection("procedures", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'procedure_code',
        'name',
        'procedure_type',
        'duration_minutes',
        'price',
        'is_active',
        'requires_doctor'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        procedure_code: {
          bsonType: 'string'
        },
        name: {
          bsonType: 'string'
        },
        procedure_type: {
          'enum': [
            'diagnostic',
            'therapeutic',
            'physical_therapy',
            'laboratory',
            'imaging',
            'vaccination',
            'preventive',
            'rehabilitation',
            'emergency'
          ]
        },
        description: {
          bsonType: 'string'
        },
        duration_minutes: {
          bsonType: 'int',
          minimum: 5
        },
        price: {
          bsonType: 'decimal',
          minimum: 0
        },
        room_type_required: {
          bsonType: [
            'string',
            'null'
          ]
        },
        equipment_required: {
          bsonType: 'array',
          items: {
            bsonType: 'string'
          }
        },
        contraindications: {
          bsonType: 'array',
          items: {
            bsonType: 'string'
          }
        },
        preparation_instructions: {
          bsonType: 'string'
        },
        is_active: {
          bsonType: 'bool'
        },
        requires_doctor: {
          bsonType: 'bool'
        },
        max_per_day: {
          bsonType: [
            'int',
            'null'
          ],
          minimum: 1
        }
      }
    }
  }
});

// rooms
db.createCollection("rooms", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'room_number',
        'room_type',
        'floor',
        'is_active'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        room_number: {
          bsonType: 'string'
        },
        room_type: {
          'enum': [
            'consultation',
            'procedure',
            'physical_therapy',
            'diagnostic',
            'vaccination',
            'adminstrative',
            'reception',
            'laboratory',
            'ultrasound',
            'doctor_office'
          ]
        },
        floor: {
          bsonType: 'int',
          minimum: -1
        },
        capacity: {
          bsonType: 'int',
          minimum: 1
        },
        equipment: {
          bsonType: 'array',
          items: {
            bsonType: 'string'
          }
        },
        is_active: {
          bsonType: 'bool'
        }
      }
    }
  }
});

// schedules
db.createCollection("schedules", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'entity_type',
        'entity_id',
        'day_of_week',
        'shift',
        'start_time',
        'end_time',
        'effective_from'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        entity_type: {
          'enum': [
            'doctor',
            'room'
          ]
        },
        entity_id: {
          bsonType: 'objectId'
        },
        day_of_week: {
          bsonType: 'int',
          minimum: 1,
          maximum: 7
        },
        shift: {
          'enum': [
            'first',
            'second',
            'full'
          ]
        },
        start_time: {
          bsonType: 'string',
          pattern: '^([01]?[0-9]|2[0-3]):[0-5][0-9]$'
        },
        end_time: {
          bsonType: 'string',
          pattern: '^([01]?[0-9]|2[0-3]):[0-5][0-9]$'
        },
        room_id: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        effective_from: {
          bsonType: 'date'
        },
        effective_until: {
          bsonType: [
            'date',
            'null'
          ]
        }
      }
    }
  }
});

// specialties
db.createCollection("specialties", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'name',
        'code'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        name: {
          bsonType: 'string'
        },
        code: {
          bsonType: 'string',
          minLength: 2,
          maxLength: 10
        },
        description: {
          bsonType: 'string'
        },
        is_therapist: {
          bsonType: 'bool'
        }
      }
    }
  }
});

// users
db.createCollection("users", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'login',
        'password_hash',
        'role',
        'full_name',
        'created_date',
        'is_active'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        login: {
          bsonType: 'string',
          minLength: 3,
          maxLength: 50
        },
        password_hash: {
          bsonType: 'string'
        },
        role: {
          'enum': [
            'administrator',
            'operator',
            'authorized',
            'guest'
          ]
        },
        full_name: {
          bsonType: 'string'
        },
        email: {
          bsonType: 'string',
          pattern: '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$'
        },
        phone: {
          bsonType: 'string'
        },
        created_date: {
          bsonType: 'date'
        },
        last_login: {
          bsonType: [
            'date',
            'null'
          ]
        },
        is_active: {
          bsonType: 'bool'
        },
        access_rights: {
          bsonType: 'object',
          properties: {
            view_data: {
              bsonType: 'bool'
            },
            edit_data: {
              bsonType: 'bool'
            },
            delete_data: {
              bsonType: 'bool'
            },
            run_aggregations: {
              bsonType: 'bool'
            },
            save_results: {
              bsonType: 'bool'
            },
            manage_users: {
              bsonType: 'bool'
            }
          }
        }
      }
    }
  }
});

// vaccinations
db.createCollection("vaccinations", {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: [
        'patient_id',
        'vaccine_name',
        'scheduled_date',
        'status'
      ],
      properties: {
        _id: {
          bsonType: 'objectId'
        },
        patient_id: {
          bsonType: 'objectId'
        },
        vaccine_name: {
          bsonType: 'string'
        },
        vaccine_lot: {
          bsonType: [
            'string',
            'null'
          ]
        },
        scheduled_date: {
          bsonType: 'date'
        },
        administered_date: {
          bsonType: [
            'date',
            'null'
          ]
        },
        administered_by: {
          bsonType: [
            'objectId',
            'null'
          ]
        },
        status: {
          'enum': [
            'scheduled',
            'completed',
            'missed',
            'contraindicated'
          ]
        },
        dose_number: {
          bsonType: 'int',
          minimum: 1
        },
        notes: {
          bsonType: [
            'string',
            'null'
          ]
        }
      }
    }
  }
});

print("All collections created successfully with validators!");