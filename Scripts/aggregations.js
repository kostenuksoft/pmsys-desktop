// Виведення графіка терапевтів з інформацією про кабінети
db.schedules.aggregate([
  {
    $match: {
      entity_type: "doctor",
      $or: [
        { effective_until: null },
        { effective_until: { $gte: new Date() } }
      ]
    }
  },
  {
    $lookup: {
      from: "doctors",
      localField: "entity_id",
      foreignField: "_id",
      as: "doctor_info"
    }
  },
  { $unwind: "$doctor_info" },
  {
    $lookup: {
      from: "specialties",
      localField: "doctor_info.specialty_id",
      foreignField: "_id",
      as: "specialty_info"
    }
  },
  { $unwind: "$specialty_info" },
  {
    $match: {
      "specialty_info.is_therapist": true
    }
  },
  {
    $lookup: {
      from: "rooms",
      localField: "room_id",
      foreignField: "_id",
      as: "room_info"
    }
  },
  { $unwind: { path: "$room_info", preserveNullAndEmptyArrays: true } },
  {
    $project: {
      _id: 0,
      "ПІБ лікаря": "$doctor_info.full_name",
      "Спеціальність": "$specialty_info.name",
      "День тижня": {
        $switch: {
          branches: [
            { case: { $eq: ["$day_of_week", 1] }, then: "Понеділок" },
            { case: { $eq: ["$day_of_week", 2] }, then: "Вівторок" },
            { case: { $eq: ["$day_of_week", 3] }, then: "Середа" },
            { case: { $eq: ["$day_of_week", 4] }, then: "Четвер" },
            { case: { $eq: ["$day_of_week", 5] }, then: "П'ятниця" },
            { case: { $eq: ["$day_of_week", 6] }, then: "Субота" },
            { case: { $eq: ["$day_of_week", 7] }, then: "Неділя" }
          ],
          default: "Невідомо"
        }
      },
      "Час початку": "$start_time",
      "Час кінця": "$end_time",
      "Зміна": "$shift",
      "Кабінет №": "$room_info.room_number",
      "Тип кабінету": "$room_info.room_type",
      "Поверх": "$room_info.floor"
    }
  },
  {
    $sort: {
      "day_of_week": 1,
      "Час початку": 1
    }
  }
]);


// Інформація про лікарів, довідки та кількість пацієнтів за тиждень
db.doctors.aggregate([
  {
    $match: {
      is_active: true
    }
  },
  {
    $lookup: {
      from: "specialties",
      localField: "specialty_id",
      foreignField: "_id",
      as: "specialty"
    }
  },
  { $unwind: "$specialty" },
  {
    $lookup: {
      from: "certificates",
      localField: "_id",
      foreignField: "doctor_id",
      as: "issued_certificates"
    }
  },
  {
    $lookup: {
      from: "appointments",
      let: { doctor_id: "$_id" },
      pipeline: [
        {
          $match: {
            $expr: {
              $and: [
                { $eq: ["$doctor_id", "$$doctor_id"] },
                {
                  $gte: [
                    "$appointment_date",
                    new Date(new Date().setDate(new Date().getDate() - 7))
                  ]
                }
              ]
            }
          }
        }
      ],
      as: "weekly_appointments"
    }
  },
  {
    $addFields: {
      unique_patients_week: {
        $size: {
          $setUnion: {
            $map: {
              input: "$weekly_appointments",
              as: "apt",
              in: "$$apt.patient_id"
            }
          }
        }
      }
    }
  },
  {
    $project: {
      _id: 0,
      "Табельний номер": "$employee_number",
      "ПІБ": "$full_name",
      "Спеціальність": "$specialty.name",
      "Категорія": "$category",
      "Стаж (років)": "$experience_years",
      "Телефон": "$phone",
      "Email": "$email",
      "Дільничний лікар": "$is_district_doctor",
      "Дільниця": "$district_area",
      "Видано довідок (всього)": { $size: "$issued_certificates" },
      "Довідки за типами": {
        $arrayToObject: {
          $map: {
            input: {
              $setUnion: {
                $map: {
                  input: "$issued_certificates",
                  as: "cert",
                  in: "$$cert.type"
                }
              }
            },
            as: "type",
            in: {
              k: "$$type",
              v: {
                $size: {
                  $filter: {
                    input: "$issued_certificates",
                    as: "cert",
                    cond: { $eq: ["$$cert.type", "$$type"] }
                  }
                }
              }
            }
          }
        }
      },
      "Пацієнтів за тиждень": "$unique_patients_week"
    }
  },
  {
    $sort: { "ПІБ": 1 }
  }
]);


db.patients.aggregate([
  {
    $match: {
      // Розкоментувати потрібний фільтр (:
      // full_name: /Іванов/i,  // за прізвищем
      // medical_record_number: "P0000001",  // за номером картки
      // health_status: "chronic",  // за станом здоров'я
      // assigned_doctor_id: ObjectId("..."),  // за лікарем
      is_active: true
    }
  },
  {
    $lookup: {
      from: "doctors",
      localField: "assigned_doctor_id",
      foreignField: "_id",
      as: "assigned_doctor"
    }
  },
  {
    $unwind: {
      path: "$assigned_doctor",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $lookup: {
      from: "specialties",
      localField: "assigned_doctor.specialty_id",
      foreignField: "_id",
      as: "doctor_specialty"
    }
  },
  {
    $unwind: {
      path: "$doctor_specialty",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $project: {
      _id: 0,
      "Номер медкартки": "$medical_record_number",
      "ПІБ пацієнта": "$full_name",
      "Дата народження": {
        $dateToString: {
          format: "%d.%m.%Y",
          date: "$birth_date"
        }
      },
      "Вік": {
        $floor: {
          $divide: [
            { $subtract: [new Date(), "$birth_date"] },
            31536000000
          ]
        }
      },
      "Стать": "$gender",
      "Адреса": {
        $concat: [
          "вул. ", "$address.street", ", ",
          "буд. ", "$address.building",
          { $ifNull: [{ $concat: [", кв. ", "$address.apartment"] }, ""] },
          ", ", "$address.city"
        ]
      },
      "Телефон": "$phone",
      "Група крові": "$blood_type",
      "Стан здоров'я": "$health_status",
      "Алергії": {
        $cond: {
          if: { $gt: [{ $size: { $ifNull: ["$allergies", []] } }, 0] },
          then: { $reduce: {
            input: "$allergies",
            initialValue: "",
            in: {
              $concat: [
                "$$value",
                { $cond: [{ $eq: ["$$value", ""] }, "", ", "] },
                "$$this"
              ]
            }
          }},
          else: "Немає"
        }
      },
      "Закріплений лікар": "$assigned_doctor.full_name",
      "Спеціальність лікаря": "$doctor_specialty.name",
      "Дата реєстрації": {
        $dateToString: {
          format: "%d.%m.%Y",
          date: "$registration_date"
        }
      }
    }
  },
  {
    $sort: { "ПІБ пацієнта": 1 }
  }
]);

// Частина А: Пацієнти, які відвідали >2 лікарів за тиждень
db.appointments.aggregate([
  {
    $match: {
      appointment_date: {
        $gte: new Date(new Date().setDate(new Date().getDate() - 7)),
        $lte: new Date()
      },
      status: { $in: ["completed", "in_progress"] }
    }
  },
  {
    $group: {
      _id: "$patient_id",
      unique_doctors: { $addToSet: "$doctor_id" },
      appointments_count: { $sum: 1 }
    }
  },
  {
    $match: {
      $expr: { $gt: [{ $size: "$unique_doctors" }, 2] }
    }
  },
  {
    $lookup: {
      from: "patients",
      localField: "_id",
      foreignField: "_id",
      as: "patient"
    }
  },
  { $unwind: "$patient" },
  {
    $lookup: {
      from: "doctors",
      localField: "unique_doctors",
      foreignField: "_id",
      as: "doctors_list"
    }
  },
  {
    $project: {
      _id: 0,
      "Медкартка": "$patient.medical_record_number",
      "ПІБ пацієнта": "$patient.full_name",
      "Кількість різних лікарів": { $size: "$unique_doctors" },
      "Загальна кількість візитів": "$appointments_count",
      "Лікарі": {
        $map: {
          input: "$doctors_list",
          as: "doc",
          in: "$$doc.full_name"
        }
      }
    }
  },
  {
    $sort: { "Кількість різних лікарів": -1 }
  }
]);

// Частина Б: Кількість хворих з діагнозом "ангіна" за місяць
db.examinations.aggregate([
  {
    $match: {
      examination_date: {
        $gte: new Date(new Date().setMonth(new Date().getMonth() - 1)),
        $lte: new Date()
      }
    }
  },
  {
    $lookup: {
      from: "diagnoses",
      localField: "diagnosis_ids",
      foreignField: "_id",
      as: "diagnoses"
    }
  },
  {
    $match: {
      "diagnoses.name": /ангіна|тонзиліт/i
    }
  },
  {
    $group: {
      _id: null,
      unique_patients: { $addToSet: "$patient_id" },
      total_examinations: { $sum: 1 }
    }
  },
  {
    $project: {
      _id: 0,
      "Період": "Останній місяць",
      "Кількість унікальних хворих з ангіною": { $size: "$unique_patients" },
      "Загальна кількість оглядів": "$total_examinations"
    }
  }
]);

// Частина А: Графік роботи конкретного лікаря на тиждень/місяць
db.schedules.aggregate([
  {
    $match: {
      entity_type: "doctor",
      entity_id: ObjectId("DOCTOR_ID_HERE"), // Замінити на ID лікаря
      $or: [
        { effective_until: null },
        { effective_until: { $gte: new Date() } }
      ]
    }
  },
  {
    $lookup: {
      from: "doctors",
      localField: "entity_id",
      foreignField: "_id",
      as: "doctor"
    }
  },
  { $unwind: "$doctor" },
  {
    $lookup: {
      from: "rooms",
      localField: "room_id",
      foreignField: "_id",
      as: "room"
    }
  },
  {
    $unwind: {
      path: "$room",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $lookup: {
      from: "specialties",
      localField: "doctor.specialty_id",
      foreignField: "_id",
      as: "specialty"
    }
  },
  { $unwind: "$specialty" },
  {
    $project: {
      _id: 0,
      "Лікар": "$doctor.full_name",
      "Табельний №": "$doctor.employee_number",
      "Спеціальність": "$specialty.name",
      "День тижня": "$day_of_week",
      "День (текст)": {
        $arrayElemAt: [
          ["Неділя", "Понеділок", "Вівторок", "Середа", "Четвер", "П'ятниця", "Субота"],
          "$day_of_week"
        ]
      },
      "Зміна": "$shift",
      "Початок": "$start_time",
      "Кінець": "$end_time",
      "Кабінет": "$room.room_number",
      "Тип кабінету": "$room.room_type",
      "Дійсно з": {
        $dateToString: {
          format: "%d.%m.%Y",
          date: "$effective_from"
        }
      },
      "Дійсно до": {
        $cond: {
          if: { $ne: ["$effective_until", null] },
          then: {
            $dateToString: {
              format: "%d.%m.%Y",
              date: "$effective_until"
            }
          },
          else: "Безстроково"
        }
      }
    }
  },
  {
    $sort: { "День тижня": 1, "Початок": 1 }
  }
]);

// Частина Б: Перелік та кількість лікарів за спеціальністю
db.doctors.aggregate([
  {
    $match: {
      is_active: true,
      specialty_id: ObjectId("SPECIALTY_ID_HERE") // Замінити на ID спеціальності
    }
  },
  {
    $lookup: {
      from: "specialties",
      localField: "specialty_id",
      foreignField: "_id",
      as: "specialty"
    }
  },
  { $unwind: "$specialty" },
  {
    $facet: {
      "Список лікарів": [
        {
          $project: {
            _id: 0,
            "Табельний №": "$employee_number",
            "ПІБ": "$full_name",
            "Категорія": "$category",
            "Стаж": "$experience_years",
            "Дільничний": "$is_district_doctor",
            "Телефон": "$phone"
          }
        },
        { $sort: { "ПІБ": 1 } }
      ],
      "Статистика": [
        {
          $group: {
            _id: "$specialty.name",
            "Загальна кількість": { $sum: 1 },
            "З категорією": {
              $sum: {
                $cond: [
                  { $ne: ["$category", "none"] },
                  1,
                  0
                ]
              }
            },
            "Дільничних лікарів": {
              $sum: {
                $cond: ["$is_district_doctor", 1, 0]
              }
            },
            "Середній стаж": { $avg: "$experience_years" }
          }
        },
        {
          $project: {
            _id: 0,
            "Спеціальність": "$_id",
            "Загальна кількість": 1,
            "З категорією": 1,
            "Дільничних лікарів": 1,
            "Середній стаж (років)": { $round: ["$Середній стаж", 1] }
          }
        }
      ]
    }
  }
]);

// Частина А: Пацієнти, які викликали лікаря додому
db.home_visits.aggregate([
  {
    $match: {
      status: { $in: ["assigned", "in_progress", "completed"] }
    }
  },
  {
    $lookup: {
      from: "patients",
      localField: "patient_id",
      foreignField: "_id",
      as: "patient"
    }
  },
  {
    $unwind: {
      path: "$patient",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $lookup: {
      from: "doctors",
      localField: "assigned_doctor_id",
      foreignField: "_id",
      as: "doctor"
    }
  },
  {
    $unwind: {
      path: "$doctor",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $project: {
      _id: 0,
      "ПІБ пацієнта": {
        $ifNull: ["$patient.full_name", "$patient_name"]
      },
      "Медкартка": "$patient.medical_record_number",
      "Адреса виклику": "$address",
      "Телефон": "$phone",
      "Альтернативний телефон": "$alternative_phone",
      "Дата виклику": {
        $dateToString: {
          format: "%d.%m.%Y",
          date: "$call_date"
        }
      },
      "Час виклику": "$call_time",
      "Терміновість": {
        $switch: {
          branches: [
            { case: { $eq: ["$urgency", "regular"] }, then: "Звичайний" },
            { case: { $eq: ["$urgency", "urgent"] }, then: "Терміновий" },
            { case: { $eq: ["$urgency", "emergency"] }, then: "Екстрений" }
          ],
          default: "$urgency"
        }
      },
      "Симптоми": "$symptoms",
      "Призначений лікар": "$doctor.full_name",
      "Табельний № лікаря": "$doctor.employee_number",
      "Дата візиту": {
        $cond: {
          if: { $ne: ["$visit_date", null] },
          then: {
            $dateToString: {
              format: "%d.%m.%Y",
              date: "$visit_date"
            }
          },
          else: "Не призначено"
        }
      },
      "Часовий слот": "$visit_time_slot",
      "Статус": {
        $switch: {
          branches: [
            { case: { $eq: ["$status", "new"] }, then: "Новий" },
            { case: { $eq: ["$status", "assigned"] }, then: "Призначено" },
            { case: { $eq: ["$status", "in_progress"] }, then: "В процесі" },
            { case: { $eq: ["$status", "completed"] }, then: "Завершено" },
            { case: { $eq: ["$status", "cancelled"] }, then: "Скасовано" }
          ],
          default: "$status"
        }
      },
      "Примітки": "$notes"
    }
  },
  {
    $sort: {
      "Дата виклику": -1,
      "Терміновість": 1
    }
  }
]);

// Частина Б: Кількість викликів, прийнятих кожним лікарем
db.home_visits.aggregate([
  {
    $match: {
      assigned_doctor_id: { $ne: null },
      status: { $ne: "cancelled" }
    }
  },
  {
    $group: {
      _id: "$assigned_doctor_id",
      total_calls: { $sum: 1 },
      completed_calls: {
        $sum: {
          $cond: [{ $eq: ["$status", "completed"] }, 1, 0]
        }
      },
      in_progress_calls: {
        $sum: {
          $cond: [{ $eq: ["$status", "in_progress"] }, 1, 0]
        }
      },
      emergency_calls: {
        $sum: {
          $cond: [{ $eq: ["$urgency", "emergency"] }, 1, 0]
        }
      },
      urgent_calls: {
        $sum: {
          $cond: [{ $eq: ["$urgency", "urgent"] }, 1, 0]
        }
      }
    }
  },
  {
    $lookup: {
      from: "doctors",
      localField: "_id",
      foreignField: "_id",
      as: "doctor"
    }
  },
  { $unwind: "$doctor" },
  {
    $lookup: {
      from: "specialties",
      localField: "doctor.specialty_id",
      foreignField: "_id",
      as: "specialty"
    }
  },
  { $unwind: "$specialty" },
  {
    $project: {
      _id: 0,
      "Табельний №": "$doctor.employee_number",
      "ПІБ лікаря": "$doctor.full_name",
      "Спеціальність": "$specialty.name",
      "Дільничний лікар": "$doctor.is_district_doctor",
      "Дільниця": "$doctor.district_area",
      "Всього викликів": "$total_calls",
      "Завершено": "$completed_calls",
      "В роботі": "$in_progress_calls",
      "Екстрених": "$emergency_calls",
      "Термінових": "$urgent_calls",
      "Звичайних": {
        $subtract: [
          "$total_calls",
          { $add: ["$emergency_calls", "$urgent_calls"] }
        ]
      }
    }
  },
  {
    $sort: { "Всього викликів": -1 }
  }
]);

// Частина А: Перелік лікувальних процедур поліклініки
db.procedures.aggregate([
  {
    $match: {
      is_active: true
    }
  },
  {
    $project: {
      _id: 0,
      "Код процедури": "$procedure_code",
      "Назва": "$name",
      "Тип процедури": {
        $switch: {
          branches: [
            { case: { $eq: ["$procedure_type", "diagnostic"] }, then: "Діагностична" },
            { case: { $eq: ["$procedure_type", "therapeutic"] }, then: "Лікувальна" },
            { case: { $eq: ["$procedure_type", "physical_therapy"] }, then: "Фізіотерапія" },
            { case: { $eq: ["$procedure_type", "laboratory"] }, then: "Лабораторна" },
            { case: { $eq: ["$procedure_type", "imaging"] }, then: "Діагностична візуалізація" },
            { case: { $eq: ["$procedure_type", "vaccination"] }, then: "Вакцинація" },
            { case: { $eq: ["$procedure_type", "preventive"] }, then: "Профілактична" },
            { case: { $eq: ["$procedure_type", "rehabilitation"] }, then: "Реабілітаційна" },
            { case: { $eq: ["$procedure_type", "emergency"] }, then: "Екстрена" }
          ],
          default: "$procedure_type"
        }
      },
      "Опис": "$description",
      "Тривалість (хв)": "$duration_minutes",
      "Вартість (грн)": { $toDouble: "$price" },
      "Потрібен лікар": "$requires_doctor",
      "Тип кабінету": "$room_type_required",
      "Необхідне обладнання": {
        $cond: {
          if: { $gt: [{ $size: { $ifNull: ["$equipment_required", []] } }, 0] },
          then: {
            $reduce: {
              input: "$equipment_required",
              initialValue: "",
              in: {
                $concat: [
                  "$$value",
                  { $cond: [{ $eq: ["$$value", ""] }, "", ", "] },
                  "$$this"
                ]
              }
            }
          },
          else: "Не потрібно"
        }
      },
      "Протипоказання": {
        $cond: {
          if: { $gt: [{ $size: { $ifNull: ["$contraindications", []] } }, 0] },
          then: {
            $reduce: {
              input: "$contraindications",
              initialValue: "",
              in: {
                $concat: [
                  "$$value",
                  { $cond: [{ $eq: ["$$value", ""] }, "", "; "] },
                  "$$this"
                ]
              }
            }
          },
          else: "Немає"
        }
      },
      "Макс. на день": "$max_per_day"
    }
  },
  {
    $sort: { "Тип процедури": 1, "Назва": 1 }
  }
]);

// Частина Б: Загальна кількість процедур за тиждень
db.patient_procedures.aggregate([
  {
    $match: {
      performed_date: {
        $gte: new Date(new Date().setDate(new Date().getDate() - 7)),
        $lte: new Date()
      },
      status: "completed"
    }
  },
  {
    $lookup: {
      from: "procedures",
      localField: "procedure_id",
      foreignField: "_id",
      as: "procedure"
    }
  },
  { $unwind: "$procedure" },
  {
    $group: {
      _id: "$procedure.procedure_type",
      procedure_type_name: { $first: "$procedure.procedure_type" },
      total_count: { $sum: 1 },
      procedures_breakdown: {
        $push: {
          name: "$procedure.name",
          code: "$procedure.procedure_code"
        }
      }
    }
  },
  {
    $project: {
      _id: 0,
      "Тип процедури": {
        $switch: {
          branches: [
            { case: { $eq: ["$procedure_type_name", "diagnostic"] }, then: "Діагностична" },
            { case: { $eq: ["$procedure_type_name", "therapeutic"] }, then: "Лікувальна" },
            { case: { $eq: ["$procedure_type_name", "physical_therapy"] }, then: "Фізіотерапія" },
            { case: { $eq: ["$procedure_type_name", "laboratory"] }, then: "Лабораторна" },
            { case: { $eq: ["$procedure_type_name", "imaging"] }, then: "Діагностична візуалізація" },
            { case: { $eq: ["$procedure_type_name", "vaccination"] }, then: "Вакцинація" },
            { case: { $eq: ["$procedure_type_name", "preventive"] }, then: "Профілактична" },
            { case: { $eq: ["$procedure_type_name", "rehabilitation"] }, then: "Реабілітаційна" },
            { case: { $eq: ["$procedure_type_name", "emergency"] }, then: "Екстрена" }
          ],
          default: "$procedure_type_name"
        }
      },
      "Загальна кількість": "$total_count",
      "Деталізація": {
        $reduce: {
          input: {
            $map: {
              input: {
                $setUnion: {
                  $map: {
                    input: "$procedures_breakdown",
                    as: "p",
                    in: { name: "$$p.name", code: "$$p.code" }
                  }
                }
              },
              as: "proc",
              in: {
                $concat: [
                  "$$proc.name",
                  " (",
                  "$$proc.code",
                  "): ",
                  { $toString: {
                    $size: {
                      $filter: {
                        input: "$procedures_breakdown",
                        as: "pb",
                        cond: { $eq: ["$$pb.code", "$$proc.code"] }
                      }
                    }
                  }}
                ]
              }
            }
          },
          initialValue: "",
          in: {
            $concat: [
              "$$value",
              { $cond: [{ $eq: ["$$value", ""] }, "", "; "] },
              "$$this"
            ]
          }
        }
      }
    }
  },
  {
    $group: {
      _id: null,
      total_all_procedures: { $sum: "$Загальна кількість" },
      by_type: { $push: "$$ROOT" }
    }
  },
  {
    $project: {
      _id: 0,
      "Період": "Останній тиждень",
      "Всього процедур виконано": "$total_all_procedures",
      "Розподіл за типами": "$by_type"
    }
  }
]);

// Частина В: Пацієнти, які отримали процедури за тиждень
db.patient_procedures.aggregate([
  {
    $match: {
      performed_date: {
        $gte: new Date(new Date().setDate(new Date().getDate() - 7)),
        $lte: new Date()
      },
      status: "completed"
    }
  },
  {
    $group: {
      _id: "$patient_id",
      procedures_count: { $sum: 1 },
      procedures_list: {
        $push: {
          procedure_id: "$procedure_id",
          date: "$performed_date"
        }
      }
    }
  },
  {
    $lookup: {
      from: "patients",
      localField: "_id",
      foreignField: "_id",
      as: "patient"
    }
  },
  { $unwind: "$patient" },
  {
    $lookup: {
      from: "procedures",
      localField: "procedures_list.procedure_id",
      foreignField: "_id",
      as: "procedures_details"
    }
  },
  {
    $project: {
      _id: 0,
      "Медкартка": "$patient.medical_record_number",
      "ПІБ пацієнта": "$patient.full_name",
      "Телефон": "$patient.phone",
      "Адреса": {
        $concat: [
          "вул. ", "$patient.address.street", ", ",
          "буд. ", "$patient.address.building"
        ]
      },
      "Кількість процедур": "$procedures_count",
      "Перелік процедур": {
        $map: {
          input: "$procedures_details",
          as: "proc",
          in: "$$proc.name"
        }
      }
    }
  },
  {
    $sort: { "Кількість процедур": -1, "ПІБ пацієнта": 1 }
  }
]);

// Частина А: Пацієнти, які робили флюорографію в заданий день
db.patient_procedures.aggregate([
  {
    $match: {
      performed_date: {
        $gte: new Date("2024-11-24T00:00:00Z"), // Замінити на потрібну дату
        $lt: new Date("2024-11-25T00:00:00Z")
      },
      status: "completed"
    }
  },
  {
    $lookup: {
      from: "procedures",
      localField: "procedure_id",
      foreignField: "_id",
      as: "procedure"
    }
  },
  { $unwind: "$procedure" },
  {
    $match: {
      "procedure.name": /флюорографія|рентген.*грудн/i
    }
  },
  {
    $lookup: {
      from: "patients",
      localField: "patient_id",
      foreignField: "_id",
      as: "patient"
    }
  },
  { $unwind: "$patient" },
  {
    $lookup: {
      from: "doctors",
      localField: "performed_by",
      foreignField: "_id",
      as: "doctor"
    }
  },
  {
    $unwind: {
      path: "$doctor",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $lookup: {
      from: "rooms",
      localField: "room_id",
      foreignField: "_id",
      as: "room"
    }
  },
  {
    $unwind: {
      path: "$room",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $project: {
      _id: 0,
      "Медкартка": "$patient.medical_record_number",
      "ПІБ пацієнта": "$patient.full_name",
      "Дата народження": {
        $dateToString: {
          format: "%d.%m.%Y",
          date: "$patient.birth_date"
        }
      },
      "Процедура": "$procedure.name",
      "Код процедури": "$procedure.procedure_code",
      "Дата виконання": {
        $dateToString: {
          format: "%d.%m.%Y %H:%M",
          date: "$performed_date"
        }
      },
      "Виконав": "$doctor.full_name",
      "Кабінет": "$room.room_number",
      "Результати": "$results",
      "Примітки": "$notes"
    }
  },
  {
    $sort: { "Дата виконання": 1 }
  }
]);

// Частина Б: Пацієнти, які не пройшли планове щеплення
db.vaccinations.aggregate([
  {
    $match: {
      status: { $in: ["scheduled", "missed"] },
      scheduled_date: { $lt: new Date() }
    }
  },
  {
    $lookup: {
      from: "patients",
      localField: "patient_id",
      foreignField: "_id",
      as: "patient"
    }
  },
  { $unwind: "$patient" },
  {
    $lookup: {
      from: "doctors",
      localField: "patient.assigned_doctor_id",
      foreignField: "_id",
      as: "assigned_doctor"
    }
  },
  {
    $unwind: {
      path: "$assigned_doctor",
      preserveNullAndEmptyArrays: true
    }
  },
  {
    $addFields: {
      days_overdue: {
        $floor: {
          $divide: [
            { $subtract: [new Date(), "$scheduled_date"] },
            86400000
          ]
        }
      }
    }
  },
  {
    $project: {
      _id: 0,
      "Медкартка": "$patient.medical_record_number",
      "ПІБ пацієнта": "$patient.full_name",
      "Дата народження": {
        $dateToString: {
          format: "%d.%m.%Y",
          date: "$patient.birth_date"
        }
      },
      "Вік (років)": {
        $floor: {
          $divide: [
            { $subtract: [new Date(), "$patient.birth_date"] },
            31536000000
          ]
        }
      },
      "Телефон": "$patient.phone",
      "Адреса": {
        $concat: [
          "вул. ", "$patient.address.street", ", ",
          "буд. ", "$patient.address.building",
          { $ifNull: [{ $concat: [", кв. ", "$patient.address.apartment"] }, ""] }
        ]
      },
      "Назва вакцини": "$vaccine_name",
      "Номер дози": "$dose_number",
      "Заплановано на": {
        $dateToString: {
          format: "%d.%m.%Y",
          date: "$scheduled_date"
        }
      },
      "Прострочено (днів)": "$days_overdue",
      "Статус": {
        $switch: {
          branches: [
            { case: { $eq: ["$status", "scheduled"] }, then: "Заплановано (не з'явився)" },
            { case: { $eq: ["$status", "missed"] }, then: "Пропущено" },
            { case: { $eq: ["$status", "contraindicated"] }, then: "Протипоказано" }
          ],
          default: "$status"
        }
      },
      "Закріплений лікар": "$assigned_doctor.full_name",
      "Телефон лікаря": "$assigned_doctor.phone",
      "Примітки": "$notes"
    }
  },
  {
    $sort: { "Прострочено (днів)": -1 }
  }
]);

// Частина А: Повна інформація про фізіотерапевтичні кабінети
db.rooms.aggregate([
  {
    $match: {
      room_type: "physical_therapy",
      is_active: true
    }
  },
  {
    $lookup: {
      from: "schedules",
      let: { room_id: "$_id" },
      pipeline: [
        {
          $match: {
            $expr: {
              $and: [
                { $eq: ["$entity_type", "room"] },
                { $eq: ["$entity_id", "$$room_id"] },
                {
                  $or: [
                    { $eq: ["$effective_until", null] },
                    { $gte: ["$effective_until", new Date()] }
                  ]
                }
              ]
            }
          }
        }
      ],
      as: "schedule"
    }
  },
  {
    $project: {
      _id: 0,
      "Номер кабінету": "$room_number",
      "Тип": "Фізіотерапевтичний",
      "Поверх": "$floor",
      "Місткість": "$capacity",
      "Обладнання": {
        $cond: {
          if: { $gt: [{ $size: { $ifNull: ["$equipment", []] } }, 0] },
          then: {
            $reduce: {
              input: "$equipment",
              initialValue: "",
              in: {
                $concat: [
                  "$$value",
                  { $cond: [{ $eq: ["$$value", ""] }, "", ", "] },
                  "$$this"
                ]
              }
            }
          },
          else: "Не вказано"
        }
      },
      "Активний": "$is_active",
      "Графік роботи": {
        $map: {
          input: "$schedule",
          as: "sch",
          in: {
            "День тижня": {
              $arrayElemAt: [
                ["Неділя", "Понеділок", "Вівторок", "Середа", "Четвер", "П'ятниця", "Субота"],
                "$$sch.day_of_week"
              ]
            },
            "Зміна": "$$sch.shift",
            "Години": {
              $concat: ["$$sch.start_time", " - ", "$$sch.end_time"]
            }
          }
        }
      }
    }
  },
  {
    $sort: { "Поверх": 1, "Номер кабінету": 1 }
  }
]);

// Частина Б: Графік роботи фіз.кабінетів по змінах
db.schedules.aggregate([
  {
    $match: {
      entity_type: "room",
      $or: [
        { effective_until: null },
        { effective_until: { $gte: new Date() } }
      ]
    }
  },
  {
    $lookup: {
      from: "rooms",
      localField: "entity_id",
      foreignField: "_id",
      as: "room"
    }
  },
  { $unwind: "$room" },
  {
    $match: {
      "room.room_type": "physical_therapy",
      "room.is_active": true
    }
  },
  {
    $group: {
      _id: {
        room_id: "$room._id",
        room_number: "$room.room_number",
        shift: "$shift"
      },
      days: {
        $push: {
          day_of_week: "$day_of_week",
          start_time: "$start_time",
          end_time: "$end_time"
        }
      }
    }
  },
  {
    $project: {
      _id: 0,
      "Кабінет №": "$_id.room_number",
      "Зміна": {
        $switch: {
          branches: [
            { case: { $eq: ["$_id.shift", "first"] }, then: "Перша" },
            { case: { $eq: ["$_id.shift", "second"] }, then: "Друга" },
            { case: { $eq: ["$_id.shift", "full"] }, then: "Дві зміни" }
          ],
          default: "$_id.shift"
        }
      },
      "Робочі дні": {
        $map: {
          input: "$days",
          as: "day",
          in: {
            $concat: [
              {
                $arrayElemAt: [
                  ["Нд", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"],
                  "$$day.day_of_week"
                ]
              },
              " (",
              "$$day.start_time",
              "-",
              "$$day.end_time",
              ")"
            ]
          }
        }
      }
    }
  },
  {
    $sort: { "Кабінет №": 1, "Зміна": 1 }
  }
]);

// Частина В: Кількість лікарів у кожному фіз.кабінеті протягом тижня
db.schedules.aggregate([
  {
    $match: {
      entity_type: "doctor",
      $or: [
        { effective_until: null },
        { effective_until: { $gte: new Date() } }
      ]
    }
  },
  {
    $lookup: {
      from: "rooms",
      localField: "room_id",
      foreignField: "_id",
      as: "room"
    }
  },
  { $unwind: "$room" },
  {
    $match: {
      "room.room_type": "physical_therapy"
    }
  },
  {
    $group: {
      _id: {
        room_id: "$room._id",
        room_number: "$room.room_number"
      },
      unique_doctors: { $addToSet: "$entity_id" },
      total_shifts: { $sum: 1 }
    }
  },
  {
    $lookup: {
      from: "doctors",
      localField: "unique_doctors",
      foreignField: "_id",
      as: "doctors_list"
    }
  },
  {
    $project: {
      _id: 0,
      "Кабінет №": "$_id.room_number",
      "Кількість лікарів": { $size: "$unique_doctors" },
      "Всього змін на тиждень": "$total_shifts",
      "Лікарі": {
        $map: {
          input: "$doctors_list",
          as: "doc",
          in: {
            "ПІБ": "$$doc.full_name",
            "Табельний №": "$$doc.employee_number"
          }
        }
      }
    }
  },
  {
    $sort: { "Кількість лікарів": -1 }
  }
]);

// Комплексна агрегація: загальна статистика + по групах лікарів
db.appointments.aggregate([
  {
    $match: {
      appointment_date: {
        $gte: new Date(new Date().setMonth(new Date().getMonth() - 1)),
        $lte: new Date()
      },
      status: { $in: ["completed", "in_progress"] }
    }
  },
  {
    $lookup: {
      from: "doctors",
      localField: "doctor_id",
      foreignField: "_id",
      as: "doctor"
    }
  },
  { $unwind: "$doctor" },
  {
    $lookup: {
      from: "specialties",
      localField: "doctor.specialty_id",
      foreignField: "_id",
      as: "specialty"
    }
  },
  { $unwind: "$specialty" },
  {
    $facet: {
      "Загальна статистика": [
        {
          $group: {
            _id: null,
            total_visits: { $sum: 1 },
            unique_patients: { $addToSet: "$patient_id" },
            unique_doctors: { $addToSet: "$doctor_id" },
            by_type: {
              $push: "$type"
            }
          }
        },
        {
          $project: {
            _id: 0,
            "Період": "Останній місяць",
            "Всього відвідувань": "$total_visits",
            "Унікальних пацієнтів": { $size: "$unique_patients" },
            "Працювало лікарів": { $size: "$unique_doctors" },
            "Середньо візитів на день": {
              $round: [{ $divide: ["$total_visits", 30] }, 1]
            },
            "За типами прийому": {
              $arrayToObject: {
                $map: {
                  input: ["primary", "secondary", "checkup", "vaccination", "procedure"],
                  as: "type",
                  in: {
                    k: {
                      $switch: {
                        branches: [
                          { case: { $eq: ["$$type", "primary"] }, then: "Первинний" },
                          { case: { $eq: ["$$type", "secondary"] }, then: "Повторний" },
                          { case: { $eq: ["$$type", "checkup"] }, then: "Профогляд" },
                          { case: { $eq: ["$$type", "vaccination"] }, then: "Вакцинація" },
                          { case: { $eq: ["$$type", "procedure"] }, then: "Процедура" }
                        ],
                        default: "$$type"
                      }
                    },
                    v: {
                      $size: {
                        $filter: {
                          input: "$by_type",
                          as: "t",
                          cond: { $eq: ["$$t", "$$type"] }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      ],
      "За спеціальностями": [
        {
          $group: {
            _id: "$specialty._id",
            specialty_name: { $first: "$specialty.name" },
            specialty_code: { $first: "$specialty.code" },
            total_visits: { $sum: 1 },
            unique_patients: { $addToSet: "$patient_id" },
            doctors_count: { $addToSet: "$doctor_id" },
            by_doctor: {
              $push: {
                doctor_id: "$doctor._id",
                doctor_name: "$doctor.full_name"
              }
            }
          }
        },
        {
          $project: {
            _id: 0,
            "Спеціальність": "$specialty_name",
            "Код": "$specialty_code",
            "Всього відвідувань": "$total_visits",
            "Унікальних пацієнтів": { $size: "$unique_patients" },
            "Кількість лікарів": { $size: "$doctors_count" },
            "Середньо на лікаря": {
              $round: [
                {
                  $divide: [
                    "$total_visits",
                    { $size: "$doctors_count" }
                  ]
                },
                1
              ]
            },
            "Деталізація по лікарях": {
              $reduce: {
                input: {
                  $map: {
                    input: {
                      $setUnion: {
                        $map: {
                          input: "$by_doctor",
                          as: "d",
                          in: {
                            id: "$$d.doctor_id",
                            name: "$$d.doctor_name"
                          }
                        }
                      }
                    },
                    as: "doc",
                    in: {
                      $concat: [
                        "$$doc.name",
                        ": ",
                        {
                          $toString: {
                            $size: {
                              $filter: {
                                input: "$by_doctor",
                                as: "bd",
                                cond: { $eq: ["$$bd.doctor_id", "$$doc.id"] }
                              }
                            }
                          }
                        },
                        " візитів"
                      ]
                    }
                  }
                },
                initialValue: "",
                in: {
                  $concat: [
                    "$$value",
                    { $cond: [{ $eq: ["$$value", ""] }, "", "; "] },
                    "$$this"
                  ]
                }
              }
            }
          }
        },
        {
          $sort: { "Всього відвідувань": -1 }
        }
      ],
      "Динаміка по тижнях": [
        {
          $addFields: {
            week_number: {
              $week: "$appointment_date"
            }
          }
        },
        {
          $group: {
            _id: "$week_number",
            visits: { $sum: 1 },
            first_date: { $min: "$appointment_date" }
          }
        },
        {
          $project: {
            _id: 0,
            "Тиждень №": "$_id",
            "Дата початку": {
              $dateToString: {
                format: "%d.%m.%Y",
                date: "$first_date"
              }
            },
            "Кількість відвідувань": "$visits"
          }
        },
        {
          $sort: { "Тиждень №": 1 }
        }
      ]
    }
  }
]);