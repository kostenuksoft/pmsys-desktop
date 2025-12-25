use polyclinic_test
const DB_NAME = "polyclinic_db";
const db = db.getSiblingDB(DB_NAME);

print("connecting to database - ok");
print(`database name: ${DB_NAME}`);


function randomItem(arr) {
    return arr[Math.floor(Math.random() * arr.length)];
}

function randomInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function randomFloat(min, max, decimals = 1) {
    return parseFloat((Math.random() * (max - min) + min).toFixed(decimals));
}

function randomDate(start, end) {
    return new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
}

function formatTime(hours, minutes) {
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
}

function weightedRandom(weights) {
    const total = weights.reduce((sum, w) => sum + w, 0);
    let random = Math.random() * total;
    for (let i = 0; i < weights.length; i++) {
        if (random < weights[i]) return i;
        random -= weights[i];
    }
    return weights.length - 1;
}

print("loading helper functions - ok");

const maleFirstNames = [
    "Олександр", "Андрій", "Іван", "Василь", "Петро", "Микола", "Дмитро", 
    "Сергій", "Володимир", "Юрій", "Віктор", "Олег", "Богдан", "Ярослав",
    "Тарас", "Максим", "Роман", "Віталій", "Павло", "Григорій", "Степан",
    "Ігор", "Анатолій", "Михайло", "Артем", "Денис", "Костянтин", "Євген",
    "Федір", "Леонід", "Назар", "Святослав", "Ростислав", "Данило", "Борис",
    "Валентин", "Вадим", "Руслан", "Семен", "Захар", "Матвій", "Марк"
];

const femaleFirstNames = [
    "Олена", "Наталія", "Тетяна", "Марія", "Ірина", "Людмила", "Світлана",
    "Ганна", "Оксана", "Юлія", "Катерина", "Валентина", "Лариса", "Віра",
    "Надія", "Любов", "Галина", "Алла", "Раїса", "Ольга", "Інна", "Дарія",
    "Анастасія", "Вікторія", "Софія", "Поліна", "Зоя", "Тамара",
    "Олександра", "Антоніна", "Валерія", "Маргарита", "Христина", "Ярослава",
    "Діана", "Евеліна", "Уляна", "Соломія", "Мирослава", "Естер", "Богдана"
];

const maleMiddleNames = [
    "Олександрович", "Андрійович", "Іванович", "Васильович", "Петрович",
    "Миколайович", "Дмитрович", "Сергійович", "Володимирович", "Юрійович",
    "Вікторович", "Олегович", "Богданович", "Ярославович", "Тарасович",
    "Максимович", "Романович", "Віталійович", "Павлович", "Григорович",
    "Степанович", "Ігорович", "Анатолійович", "Михайлович", "Артемович"
];

const femaleMiddleNames = [
    "Олександрівна", "Андріївна", "Іванівна", "Василівна", "Петрівна",
    "Миколаївна", "Дмитрівна", "Сергіївна", "Володимирівна", "Юріївна",
    "Вікторівна", "Олегівна", "Богданівна", "Ярославівна", "Тарасівна",
    "Максимівна", "Романівна", "Віталіївна", "Павлівна", "Григорівна",
    "Степанівна", "Ігорівна", "Анатоліївна", "Михайлівна", "Артемівна"
];

const lastNames = [
    "Шевченко", "Коваленко", "Бондаренко", "Ткаченко", "Кравченко", "Мельник",
    "Петренко", "Іваненко", "Полтавець", "Гриценко", "Павленко", "Литвиненко",
    "Савченко", "Марченко", "Руденко", "Федоренко", "Семенченко", "Кузьменко",
    "Морозов", "Левченко", "Васильченко", "Захарченко", "Романенко", "Сидоренко",
    "Михайленко", "Коваль", "Павлюк", "Олійник", "Кравець", "Гончар", "Швець",
    "Ткач", "Ковальчук", "Поліщук", "Бойко", "Данилюк", "Яременко", "Приходько",
    "Терещенко", "Омельченко", "Лисенко", "Матвієнко", "Гордієнко",
    "Климчук", "Білоус", "Костенко", "Федорчук", "Демченко", "Рудницький"
];

const ukrainianCities = [
    "Київ", "Харків", "Одеса", "Дніпро", "Донецьк", "Запоріжжя", "Львів",
    "Кривий Ріг", "Миколаїв", "Маріуполь", "Луганськ", "Вінниця", "Макіївка",
    "Сімферополь", "Херсон", "Полтава", "Чернігів", "Черкаси", "Житомир",
    "Суми", "Хмельницький", "Чернівці", "Рівне", "Кам'янське", "Кропивницький",
    "Івано-Франківськ", "Кременчук", "Тернопіль", "Луцьк", "Біла Церква",
    "Краматорськ", "Мелітополь", "Нікополь", "Слов'янськ", "Ужгород",
    "Бердянськ", "Павлоград", "Ковель", "Умань", "Бровари", "Мукачево"
];

const streetNames = [
    "Соборна", "Шевченка", "Центральна", "Миру", "Незалежності", "Грушевського",
    "Січових Стрільців", "Франка", "Лесі Українки", "Хрещатик", "Бандери",
    "Козацька", "Київська", "Садова", "Польова", "Лісова", "Паркова", "Молодіжна",
    "Перемоги", "Робоча", "Ювілейна", "Промислова", "Зелена", "Шкільна",
    "Вокзальна", "Пушкіна", "Заводська", "Набережна", "Гагаріна", "Слобідська"
];

print("loading extended name data - ok");


const START_DATE = new Date('2025-09-01');
const END_DATE = new Date('2026-03-31');

print("date range set - ok");

print("\n" + "=".repeat(80));
print("SECTION 1: BASE STRUCTURE");
print("=".repeat(80));

const users = db.users.find().toArray();
if (users.length === 0) {
    print("fetching users - failed (no users found in database)");
    quit(1);
}
print(`fetching users - ok (found ${users.length} users)`);

print("\ngenerating specialties...");
const specialtiesData = [
    { name: "Терапія", code: "THER", description: "Загальна терапія", is_therapist: true },
    { name: "Кардіологія", code: "CARD", description: "Серцево-судинні захворювання", is_therapist: false },
    { name: "Неврологія", code: "NEUR", description: "Нервова система", is_therapist: false },
    { name: "Ендокринологія", code: "ENDO", description: "Ендокринна система", is_therapist: false },
    { name: "Гастроентерологія", code: "GAST", description: "Травна система", is_therapist: false },
    { name: "Пульмонологія", code: "PULM", description: "Дихальна система", is_therapist: false },
    { name: "Отоларингологія", code: "ENT", description: "ЛОР", is_therapist: false },
    { name: "Офтальмологія", code: "OPHT", description: "Очні захворювання", is_therapist: false },
    { name: "Дерматологія", code: "DERM", description: "Шкірні захворювання", is_therapist: false },
    { name: "Урологія", code: "UROL", description: "Сечостатева система", is_therapist: false },
    { name: "Ревматологія", code: "RHEU", description: "Опорно-рухова система", is_therapist: false },
    { name: "Хірургія", code: "SURG", description: "Хірургічне лікування", is_therapist: false }
];

try {
    db.specialties.insertMany(specialtiesData);
    print(`inserting specialties - ok (${specialtiesData.length} records)`);
} catch (e) {
    print(`inserting specialties - failed (${e.message})`);
}

const specialties = db.specialties.find().toArray();

print("\ngenerating rooms...");
const roomsData = [];
const roomTypes = ['consultation', 'procedure', 'physical_therapy', 'diagnostic', 
                   'vaccination', 'laboratory', 'ultrasound', 'doctor_office'];
let roomNum = 101;

for (let floor = 1; floor <= 3; floor++) {
    for (let i = 0; i < 8; i++) {
        const roomType = randomItem(roomTypes);
        roomsData.push({
            room_number: String(roomNum++),
            room_type: roomType,
            floor: floor,
            capacity: roomType === 'consultation' || roomType === 'doctor_office' ? 1 : randomInt(2, 4),
            equipment: [],
            is_active: true
        });
    }
}

try {
    db.rooms.insertMany(roomsData);
    print(`inserting rooms - ok (${roomsData.length} records)`);
} catch (e) {
    print(`inserting rooms - failed (${e.message})`);
}

const rooms = db.rooms.find().toArray();
const consultationRooms = rooms.filter(r => r.room_type === 'consultation' || r.room_type === 'doctor_office');

print("\ngenerating doctors...");
const doctorsData = [];
let empNum = 10001;

const districtAreas = [
    'Район №1 (вул. Соборна, Центральна)',
    'Район №2 (вул. Шевченка, Франка)',
    'Район №3 (вул. Миру, Незалежності)',
    'Район №4 (вул. Київська, Паркова)',
    'Район №5 (вул. Лісова, Польова)',
    'Район №6 (вул. Промислова, Заводська)',
    'Район №7 (вул. Набережна, Зелена)',
    'Район №8 (вул. Молодіжна, Ювілейна)'
];

const categories = ['highest', 'first', 'second', 'none'];
const categoryWeights = [0.15, 0.35, 0.30, 0.20]; 

const certificationsBySpecialty = {
    'THER': ['Сімейна медицина', 'Внутрішні хвороби', 'Невідкладна допомога'],
    'CARD': ['Кардіологія', 'Функціональна діагностика', 'ЕКГ діагностика'],
    'NEUR': ['Неврологія', 'Електронейроміографія', 'Больова терапія'],
    'ENDO': ['Ендокринологія', 'Діабетологія', 'Тиреоїдологія'],
    'GAST': ['Гастроентерологія', 'Ендоскопія', 'УЗД органів ШКТ'],
    'PULM': ['Пульмонологія', 'Спірометрія', 'Туберкульоз'],
    'ENT': ['Отоларингологія', 'Аудіометрія', 'Ендоскопія ЛОР-органів'],
    'OPHT': ['Офтальмологія', 'Лазерна корекція зору', 'Діагностика глаукоми'],
    'DERM': ['Дерматологія', 'Косметологія', 'Дерматоскопія'],
    'UROL': ['Урологія', 'Ультразвукова діагностика', 'Ендоскопічна урологія'],
    'RHEU': ['Ревматологія', 'Артроскопія', 'Мануальна терапія'],
    'SURG': ['Хірургія', 'Лапароскопічна хірургія', 'Ендоскопічна хірургія']
};

print("  ensuring coverage for all specialties...");
specialties.forEach(specialty => {
    const doctorsPerSpecialty = specialty.is_therapist ? randomInt(8, 12) : randomInt(3, 5);
    
    for (let i = 0; i < doctorsPerSpecialty; i++) {
        const isMale = Math.random() > 0.4;
        const room = randomItem(consultationRooms);
        
        const age = randomInt(25, 65);
        const birthDate = new Date();
        birthDate.setFullYear(birthDate.getFullYear() - age);
        birthDate.setMonth(randomInt(0, 11));
        birthDate.setDate(randomInt(1, 28));
        
        const hireDate = randomDate(new Date('2010-01-01'), new Date('2024-01-01'));
        
        const experienceYears = Math.max(1, new Date().getFullYear() - hireDate.getFullYear());
        
        let category;
        if (experienceYears < 3) {
            category = 'none';
        } else if (experienceYears < 7) {
            category = Math.random() > 0.5 ? 'second' : 'none';
        } else if (experienceYears < 12) {
            category = randomItem(['second', 'first']);
        } else {
            const categoryIndex = weightedRandom(categoryWeights);
            category = categories[categoryIndex];
        }
        
        const isDistrictDoctor = specialty.is_therapist && Math.random() > 0.3;
        const districtArea = isDistrictDoctor ? randomItem(districtAreas) : null;
        
        const certifications = [];
        const certList = certificationsBySpecialty[specialty.code] || ['Базова підготовка'];
        const certCount = randomInt(1, Math.min(3, certList.length));
        
        for (let c = 0; c < certCount; c++) {
            const certName = certList[c];
            const issueDate = randomDate(hireDate, new Date());
            const expiryDate = new Date(issueDate);
            expiryDate.setFullYear(expiryDate.getFullYear() + 5); 
            
            certifications.push({
                name: certName,
                issue_date: issueDate,
                expiry_date: expiryDate
            });
        }
        
        const doctor = {
            employee_number: `D${empNum++}`,
            full_name: `${randomItem(lastNames)} ${isMale ? randomItem(maleFirstNames) : randomItem(femaleFirstNames)} ${isMale ? randomItem(maleMiddleNames) : randomItem(femaleMiddleNames)}`,
            birth_date: birthDate,
            specialty_id: specialty._id,
            category: category,
            experience_years: experienceYears,
            hire_date: hireDate,
            phone: `+38050${randomInt(1000000, 9999999)}`,
            email: `doctor${empNum - 10001}@polyclinic.ua`,
            room_id: room._id,
            is_district_doctor: isDistrictDoctor,
            district_area: districtArea,
            is_active: true,
            certifications: certifications
        };
        
        doctorsData.push(doctor);
    }
});

print("  adding additional doctors for diversity...");
const additionalDoctorsCount = randomInt(5, 10);

for (let i = 0; i < additionalDoctorsCount; i++) {
    const isMale = Math.random() > 0.4;
    const specialty = randomItem(specialties);
    const room = randomItem(consultationRooms);
    
    const age = randomInt(25, 65);
    const birthDate = new Date();
    birthDate.setFullYear(birthDate.getFullYear() - age);
    birthDate.setMonth(randomInt(0, 11));
    birthDate.setDate(randomInt(1, 28));
    
    const hireDate = randomDate(new Date('2010-01-01'), new Date('2024-01-01'));
    const experienceYears = Math.max(1, new Date().getFullYear() - hireDate.getFullYear());
    
    let category;
    if (experienceYears < 3) {
        category = 'none';
    } else if (experienceYears < 7) {
        category = Math.random() > 0.5 ? 'second' : 'none';
    } else if (experienceYears < 12) {
        category = randomItem(['second', 'first']);
    } else {
        const categoryIndex = weightedRandom(categoryWeights);
        category = categories[categoryIndex];
    }
    
    const isDistrictDoctor = specialty.is_therapist && Math.random() > 0.3;
    const districtArea = isDistrictDoctor ? randomItem(districtAreas) : null;
    
    const certifications = [];
    const certList = certificationsBySpecialty[specialty.code] || ['Базова підготовка'];
    const certCount = randomInt(1, Math.min(3, certList.length));
    
    for (let c = 0; c < certCount; c++) {
        const certName = certList[c];
        const issueDate = randomDate(hireDate, new Date());
        const expiryDate = new Date(issueDate);
        expiryDate.setFullYear(expiryDate.getFullYear() + 5);
        
        certifications.push({
            name: certName,
            issue_date: issueDate,
            expiry_date: expiryDate
        });
    }
    
    doctorsData.push({
        employee_number: `D${empNum++}`,
        full_name: `${randomItem(lastNames)} ${isMale ? randomItem(maleFirstNames) : randomItem(femaleFirstNames)} ${isMale ? randomItem(maleMiddleNames) : randomItem(femaleMiddleNames)}`,
        birth_date: birthDate,
        specialty_id: specialty._id,
        category: category,
        experience_years: experienceYears,
        hire_date: hireDate,
        phone: `+38050${randomInt(1000000, 9999999)}`,
        email: `doctor${empNum - 10001}@polyclinic.ua`,
        room_id: room._id,
        is_district_doctor: isDistrictDoctor,
        district_area: districtArea,
        is_active: true,
        certifications: certifications
    });
}

try {
    db.doctors.insertMany(doctorsData);
    print(`inserting doctors - ok (${doctorsData.length} records)`);
    
    print("\n  doctors by specialty:");
    specialties.forEach(spec => {
        const count = doctorsData.filter(d => d.specialty_id.toString() === spec._id.toString()).length;
        const districtCount = doctorsData.filter(d => d.specialty_id.toString() === spec._id.toString() && d.is_district_doctor).length;
        const districtInfo = spec.is_therapist ? ` (дільничних: ${districtCount})` : '';
        print(`    ${spec.name.padEnd(25)}: ${count}${districtInfo}`);
    });
    
    print("\n  doctors by category:");
    categories.forEach(cat => {
        const count = doctorsData.filter(d => d.category === cat).length;
        const percentage = ((count / doctorsData.length) * 100).toFixed(1);
        const catName = cat === 'highest' ? 'вища' : cat === 'first' ? 'перша' : cat === 'second' ? 'друга' : 'без категорії';
        print(`    ${catName.padEnd(15)}: ${count} (${percentage}%)`);
    });
    
    print(`\n  district doctors: ${doctorsData.filter(d => d.is_district_doctor).length}`);
    print(`  doctors with certifications: ${doctorsData.filter(d => d.certifications.length > 0).length}`);
    
} catch (e) {
    print(`inserting doctors - failed (${e.message})`);
}

const doctors = db.doctors.find({ is_active: true }).toArray();

print("\ngenerating patients...");
const patientsData = [];
const bloodTypes = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];
const healthStatuses = ['healthy', 'chronic', 'acute', 'recovery', 'observation'];
let patientNum = 1000000;

for (let i = 0; i < 1000; i++) {
    const isMale = Math.random() > 0.5;
    const birthDate = randomDate(new Date('1940-01-01'), new Date('2010-01-01'));
    const registrationDate = randomDate(new Date('2020-01-01'), new Date('2025-08-01'));
    
    patientsData.push({
        medical_record_number: `P${String(patientNum++).padStart(7, '0')}`,
        full_name: `${randomItem(lastNames)} ${isMale ? randomItem(maleFirstNames) : randomItem(femaleFirstNames)} ${isMale ? randomItem(maleMiddleNames) : randomItem(femaleMiddleNames)}`,
        birth_date: birthDate,
        gender: isMale ? 'male' : 'female',
        address: {
            street: randomItem(streetNames),
            building: `${randomInt(1, 150)}`,
            apartment: Math.random() > 0.3 ? `${randomInt(1, 200)}` : null,
            entrance: Math.random() > 0.5 ? `${randomInt(1, 6)}` : null,
            floor: Math.random() > 0.5 ? randomInt(1, 16) : null,
            city: randomItem(ukrainianCities),
            district: randomItem(['Центральний', 'Шевченківський', 'Печерський', 'Подільський', 'Оболонський']),
            postal_code: `${randomInt(01000, 99999)}`
        },
        phone: `+38050${randomInt(1000000, 9999999)}`,
        alternative_phone: Math.random() > 0.6 ? `+38067${randomInt(1000000, 9999999)}` : null,
        email: Math.random() > 0.4 ? `patient${i}@email.com` : null,
        assigned_doctor_id: randomItem(doctors)._id,
        health_status: randomItem(healthStatuses),
        blood_type: Math.random() > 0.2 ? randomItem(bloodTypes) : null,
        allergies: Math.random() > 0.7 ? [randomItem(['Пеніцилін', 'Аспірин', 'Йод', 'Новокаїн'])] : [],
        registration_date: registrationDate,
        is_active: true
    });
}

try {
    db.patients.insertMany(patientsData);
    print(`inserting patients - ok (${patientsData.length} records)`);
} catch (e) {
    print(`inserting patients - failed (${e.message})`);
}

const patients = db.patients.find({ is_active: true }).toArray();

print("\ngenerating diagnoses...");
const diagnosesData = [
    { icd_code: "I10", name: "Есенціальна (первинна) гіпертензія", category: "Серцево-судинні захворювання", description: "Підвищений артеріальний тиск" },
    { icd_code: "I20.0", name: "Нестабільна стенокардія", category: "Серцево-судинні захворювання", description: "Біль у грудях при фізичному навантаженні" },
    { icd_code: "I25.1", name: "Атеросклеротична хвороба серця", category: "Серцево-судинні захворювання", description: "Звуження коронарних артерій" },
    { icd_code: "I48", name: "Фібриляція та тріпотіння передсердь", category: "Серцево-судинні захворювання", description: "Порушення серцевого ритму" },
    { icd_code: "I50.0", name: "Застійна серцева недостатність", category: "Серцево-судинні захворювання", description: "Зниження насосної функції серця" },
    { icd_code: "J00", name: "Гострий назофарингіт (нежить)", category: "Респіраторні захворювання", description: "Запалення носа та горла" },
    { icd_code: "J06.9", name: "Гостра інфекція верхніх дихальних шляхів", category: "Респіраторні захворювання", description: "ГРВІ" },
    { icd_code: "J03.9", name: "Гострий тонзиліт (ангіна)", category: "Респіраторні захворювання", description: "Запалення мигдаликів" },
    { icd_code: "J18.9", name: "Пневмонія неуточнена", category: "Респіраторні захворювання", description: "Запалення легень" },
    { icd_code: "J20.9", name: "Гострий бронхіт", category: "Респіраторні захворювання", description: "Запалення бронхів" },
    { icd_code: "J44.0", name: "Хронічне обструктивне захворювання легень", category: "Респіраторні захворювання", description: "ХОЗЛ" },
    { icd_code: "J45.0", name: "Бронхіальна астма", category: "Респіраторні захворювання", description: "Астматичні напади" },
    { icd_code: "K29.7", name: "Гастрит неуточнений", category: "Травна система", description: "Запалення слизової шлунка" },
    { icd_code: "K25.9", name: "Виразка шлунка", category: "Травна система", description: "Пептична виразка" },
    { icd_code: "K26.9", name: "Виразка дванадцятипалої кишки", category: "Травна система", description: "Дуоденальна виразка" },
    { icd_code: "K58.0", name: "Синдром подразненого кишечника з діареєю", category: "Травна система", description: "СПК" },
    { icd_code: "K80.2", name: "Жовчнокам'яна хвороба без холециститу", category: "Травна система", description: "Камені в жовчному міхурі" },
    { icd_code: "E11.9", name: "Цукровий діабет 2 типу", category: "Ендокринна система", description: "Порушення обміну глюкози" },
    { icd_code: "E03.9", name: "Гіпотиреоз неуточнений", category: "Ендокринна система", description: "Зниження функції щитовидної залози" },
    { icd_code: "E05.9", name: "Тиреотоксикоз неуточнений", category: "Ендокринна система", description: "Гіперфункція щитовидної залози" },
    { icd_code: "E66.9", name: "Ожиріння неуточнене", category: "Ендокринна система", description: "Надлишкова маса тіла" },
    { icd_code: "E78.0", name: "Чиста гіперхолестеринемія", category: "Ендокринна система", description: "Підвищений холестерин" },
    { icd_code: "M06.9", name: "Ревматоїдний артрит", category: "Опорно-рухова система", description: "Запальне захворювання суглобів" },
    { icd_code: "M15.9", name: "Поліартроз неуточнений", category: "Опорно-рухова система", description: "Ураження багатьох суглобів" },
    { icd_code: "M17.9", name: "Гонартроз", category: "Опорно-рухова система", description: "Артроз колінного суглоба" },
    { icd_code: "M42.1", name: "Остеохондроз хребта дорослих", category: "Опорно-рухова система", description: "Дегенеративні зміни хребта" },
    { icd_code: "M54.5", name: "Біль у нижній частині спини", category: "Опорно-рухова система", description: "Люмбалгія" },
    { icd_code: "M79.1", name: "Міалгія", category: "Опорно-рухова система", description: "Біль у м'язах" },
    { icd_code: "G43.9", name: "Мігрень", category: "Нервова система", description: "Пульсуючий головний біль" },
    { icd_code: "G44.2", name: "Головний біль напруження", category: "Нервова система", description: "Біль стискаючого характеру" },
    { icd_code: "G47.0", name: "Безсоння", category: "Нервова система", description: "Порушення сну" },
    { icd_code: "L20.9", name: "Атопічний дерматит", category: "Шкірні захворювання", description: "Алергічне ураження шкіри" },
    { icd_code: "L30.9", name: "Дерматит неуточнений", category: "Шкірні захворювання", description: "Запалення шкіри" },
    { icd_code: "N39.0", name: "Інфекція сечовивідних шляхів", category: "Сечостатева система", description: "Цистит, пієлонефрит" }
];

try {
    db.diagnoses.insertMany(diagnosesData);
    print(`inserting diagnoses - ok (${diagnosesData.length} records)`);
} catch (e) {
    print(`inserting diagnoses - failed (${e.message})`);
}

const diagnoses = db.diagnoses.find().toArray();

print("\ngenerating procedures...");
const proceduresData = [
    { procedure_code: "P001", name: "Масаж загальний", procedure_type: "physical_therapy", description: "Класичний лікувальний масаж", duration_minutes: 30, price: NumberDecimal("250.00"), room_type_required: "physical_therapy", equipment_required: ["Масажний стіл"], contraindications: ["Онкологія", "Тромбоз"], preparation_instructions: "Без особливої підготовки", is_active: true, requires_doctor: false, max_per_day: 20 },
    { procedure_code: "P002", name: "Електрофорез", procedure_type: "physical_therapy", description: "Введення ліків через шкіру", duration_minutes: 20, price: NumberDecimal("180.00"), room_type_required: "physical_therapy", equipment_required: ["Апарат електрофорезу"], contraindications: ["Кардіостимулятор"], preparation_instructions: "Очистити шкіру", is_active: true, requires_doctor: false, max_per_day: 30 },
    { procedure_code: "P003", name: "Ультразвукова терапія", procedure_type: "physical_therapy", description: "Лікування ультразвуком", duration_minutes: 15, price: NumberDecimal("200.00"), room_type_required: "physical_therapy", equipment_required: ["УЗ-апарат"], contraindications: ["Вагітність"], preparation_instructions: "Без підготовки", is_active: true, requires_doctor: false, max_per_day: 25 },
    { procedure_code: "D001", name: "ЕКГ", procedure_type: "diagnostic", description: "Електрокардіографія", duration_minutes: 10, price: NumberDecimal("150.00"), room_type_required: "diagnostic", equipment_required: ["Електрокардіограф"], contraindications: [], preparation_instructions: "Спокій 10 хвилин", is_active: true, requires_doctor: false, max_per_day: 40 },
    { procedure_code: "D002", name: "УЗД органів черевної порожнини", procedure_type: "imaging", description: "Ультразвукове дослідження", duration_minutes: 20, price: NumberDecimal("350.00"), room_type_required: "ultrasound", equipment_required: ["УЗД-апарат"], contraindications: [], preparation_instructions: "Натще, без газів", is_active: true, requires_doctor: true, max_per_day: 25 },
    { procedure_code: "D003", name: "Спірографія", procedure_type: "diagnostic", description: "Дослідження функції легень", duration_minutes: 15, price: NumberDecimal("200.00"), room_type_required: "diagnostic", equipment_required: ["Спірограф"], contraindications: ["Гострий інфаркт"], preparation_instructions: "Без куріння 2 год", is_active: true, requires_doctor: false, max_per_day: 20 },
    { procedure_code: "D008", name: "Флюорографія органів грудної клітки", procedure_type: "imaging", description: "Профілактичне рентгенологічне дослідження легень", duration_minutes: 10, price: NumberDecimal("150.00"), room_type_required: "diagnostic", equipment_required: ["Флюорограф"], contraindications: ["Вагітність"], preparation_instructions: "Без особливої підготовки", is_active: true, requires_doctor: true, max_per_day: 50 },
    { procedure_code: "L001", name: "Загальний аналіз крові", procedure_type: "laboratory", description: "Клінічний аналіз крові", duration_minutes: 5, price: NumberDecimal("120.00"), room_type_required: "laboratory", equipment_required: ["Аналізатор крові"], contraindications: [], preparation_instructions: "Натще", is_active: true, requires_doctor: false, max_per_day: 100 },
    { procedure_code: "L002", name: "Біохімічний аналіз крові", procedure_type: "laboratory", description: "Розширений біохімічний профіль", duration_minutes: 5, price: NumberDecimal("300.00"), room_type_required: "laboratory", equipment_required: ["Біохімічний аналізатор"], contraindications: [], preparation_instructions: "Натще 8-12 годин", is_active: true, requires_doctor: false, max_per_day: 80 },
    { procedure_code: "V001", name: "Вакцинація від грипу", procedure_type: "vaccination", description: "Щеплення проти грипу", duration_minutes: 5, price: NumberDecimal("300.00"), room_type_required: "vaccination", equipment_required: ["Холодильник"], contraindications: ["Алергія на яйця"], preparation_instructions: "Огляд лікаря", is_active: true, requires_doctor: true, max_per_day: 50 },
    { procedure_code: "P004", name: "Інгаляція", procedure_type: "therapeutic", description: "Інгаляційна терапія", duration_minutes: 10, price: NumberDecimal("100.00"), room_type_required: "procedure", equipment_required: ["Небулайзер"], contraindications: ["Кровохаркання"], preparation_instructions: "Після їжі через 1 год", is_active: true, requires_doctor: false, max_per_day: 40 }
];

try {
    db.procedures.insertMany(proceduresData);
    print(`inserting procedures - ok (${proceduresData.length} records)`);
} catch (e) {
    print(`inserting procedures - failed (${e.message})`);
}

const procedures = db.procedures.find({ is_active: true }).toArray();


print("\ngenerating schedules...");
const schedulesData = [];

doctors.forEach(doctor => {
    for (let day = 1; day <= 5; day++) {
        const shift = Math.random() > 0.5 ? 'first' : 'second';
        schedulesData.push({
            entity_type: 'doctor',
            entity_id: doctor._id,
            day_of_week: day,
            shift: shift,
            start_time: shift === 'first' ? '08:00' : '14:00',
            end_time: shift === 'first' ? '14:00' : '20:00',
            room_id: doctor.room_id,
            effective_from: new Date('2025-09-01'),
            effective_until: null
        });
    }
});

try {
    db.schedules.insertMany(schedulesData);
    print(`inserting schedules - ok (${schedulesData.length} records)`);
} catch (e) {
    print(`inserting schedules - failed (${e.message})`);
}


print("\nassigning doctors to procedure rooms...");
const procedureRooms = rooms.filter(r => 
    ['physical_therapy', 'procedure', 'diagnostic', 'laboratory', 'ultrasound', 'vaccination'].includes(r.room_type)
);

const doctorsForProcedures = doctors.filter(d => Math.random() > 0.6);
const additionalSchedules = []; 

doctorsForProcedures.forEach(doctor => {
    const numRooms = randomInt(1, 3);
    const assignedRooms = [];
    
    for (let i = 0; i < numRooms; i++) {
        const room = randomItem(procedureRooms.filter(r => 
            !assignedRooms.some(ar => ar._id.toString() === r._id.toString())
        ));
        if (!room) continue;
        
        assignedRooms.push(room);
        const workDays = randomInt(1, 3);
        
        for (let day = 1; day <= 5; day++) {
            if (Math.random() < workDays / 5) {
                const shift = randomItem(['first', 'second']);
                additionalSchedules.push({ 
                    entity_type: 'doctor',
                    entity_id: doctor._id,
                    day_of_week: day,
                    shift: shift,
                    start_time: shift === 'first' ? '08:00' : '14:00',
                    end_time: shift === 'first' ? '14:00' : '20:00',
                    room_id: room._id,
                    effective_from: new Date('2025-09-01'),
                    effective_until: null
                });
            }
        }
    }
});

if (additionalSchedules.length > 0) {
    try {
        db.schedules.insertMany(additionalSchedules);
        print(`  inserting procedure room schedules - ok (${additionalSchedules.length} records)`);
    } catch (e) {
        print(`  inserting procedure room schedules - failed (${e.message})`);
    }
}

print(`  doctors assigned to procedure rooms: ${doctorsForProcedures.length}`);


print("\ngenerating schedules for rooms...");
const roomSchedulesData = [];

const roomsNeedingSchedule = rooms.filter(r => 
    ['physical_therapy', 'procedure', 'diagnostic', 'laboratory', 'ultrasound', 'vaccination'].includes(r.room_type)
);

print(`  rooms needing schedule: ${roomsNeedingSchedule.length}`);

roomsNeedingSchedule.forEach(room => {
    let workDays;
    let shiftsPattern;
    
    if (room.room_type === 'physical_therapy') {
        workDays = randomInt(5, 6);
        const shiftType = weightedRandom([0.60, 0.25, 0.15]);
        shiftsPattern = ['full', 'first', 'second'][shiftType];
    } else if (room.room_type === 'laboratory') {
        workDays = 5;
        shiftsPattern = Math.random() > 0.7 ? 'full' : 'first';
    } else if (room.room_type === 'diagnostic' || room.room_type === 'ultrasound') {
        workDays = 5;
        shiftsPattern = randomItem(['first', 'second', 'full']);
    } else {
        workDays = randomInt(5, 6);
        shiftsPattern = randomItem(['first', 'second', 'full']);
    }
    
    const selectedDays = [];
    const possibleDays = [1, 2, 3, 4, 5, 6]; 
    
    while (selectedDays.length < workDays) {
        const day = randomItem(possibleDays.filter(d => !selectedDays.includes(d)));
        selectedDays.push(day);
    }
    
    selectedDays.sort((a, b) => a - b);
    
    selectedDays.forEach(day => {
        if (shiftsPattern === 'full') {
            roomSchedulesData.push({
                entity_type: 'room',
                entity_id: room._id,
                day_of_week: day,
                shift: 'first',
                start_time: '08:00',
                end_time: '14:00',
                room_id: null, 
                effective_from: new Date('2025-09-01'),
                effective_until: null
            });
            
            roomSchedulesData.push({
                entity_type: 'room',
                entity_id: room._id,
                day_of_week: day,
                shift: 'second',
                start_time: '14:00',
                end_time: '20:00',
                room_id: null,
                effective_from: new Date('2025-09-01'),
                effective_until: null
            });
        } else {
            roomSchedulesData.push({
                entity_type: 'room',
                entity_id: room._id,
                day_of_week: day,
                shift: shiftsPattern,
                start_time: shiftsPattern === 'first' ? '08:00' : '14:00',
                end_time: shiftsPattern === 'first' ? '14:00' : '20:00',
                room_id: null,
                effective_from: new Date('2025-09-01'),
                effective_until: null
            });
        }
    });
});

try {
    db.schedules.insertMany(roomSchedulesData);
    print(`inserting room schedules - ok (${roomSchedulesData.length} records)`);
    
    print("\n  room schedules by type:");
    const roomTypes = [...new Set(roomsNeedingSchedule.map(r => r.room_type))];
    roomTypes.forEach(type => {
        const roomsOfType = roomsNeedingSchedule.filter(r => r.room_type === type);
        const schedulesCount = roomSchedulesData.filter(s => 
            roomsOfType.some(r => r._id.toString() === s.entity_id.toString())
        ).length;
        print(`    ${type.padEnd(20)}: ${schedulesCount} schedule entries`);
    });
    
    print("\n  room schedules by shift:");
    ['first', 'second', 'full'].forEach(shift => {
        const count = roomSchedulesData.filter(s => 
            shift === 'full' ? (s.shift === 'first' || s.shift === 'second') : s.shift === shift
        ).length;
        const shiftName = shift === 'first' ? 'Перша зміна' : 
                         shift === 'second' ? 'Друга зміна' : 'Дві зміни (обидві)';
        print(`    ${shiftName.padEnd(20)}: ${count}`);
    });
    
} catch (e) {
    print(`inserting room schedules - failed (${e.message})`);
}

print("\nroom schedules generation - ok");

print("\ngenerating initial appointments...");
const appointmentsData = [];
let appointmentCount = 0;

for (let daysOffset = 0; daysOffset < 180; daysOffset++) {
    const currentDate = new Date(START_DATE.getTime() + daysOffset * 24 * 60 * 60 * 1000);
    const dayOfWeek = currentDate.getDay();
    
    if (dayOfWeek === 0 || dayOfWeek === 6) continue;
    
    const dailyAppointments = randomInt(15, 30);
    
    for (let i = 0; i < dailyAppointments; i++) {
        const patient = randomItem(patients);
        const doctor = randomItem(doctors);
        const schedule = db.schedules.findOne({ entity_id: doctor._id, day_of_week: dayOfWeek });
        
        if (!schedule) continue;
        
        const hour = schedule.shift === 'first' ? randomInt(8, 13) : randomInt(14, 19);
        const minute = randomInt(0, 3) * 15;
        
        const createdBy = randomItem(users)._id;
        
        appointmentsData.push({
            patient_id: patient._id,
            doctor_id: doctor._id,
            room_id: schedule.room_id,
            appointment_date: currentDate,
            appointment_time: formatTime(hour, minute),
            type: randomItem(['primary', 'primary', 'secondary', 'checkup']),
            status: currentDate < new Date() ? 'completed' : randomItem(['scheduled', 'scheduled', 'scheduled', 'in_progress']),
            complaints: null,
            created_date: new Date(currentDate.getTime() - randomInt(1, 7) * 24 * 60 * 60 * 1000),
            created_by: createdBy
        });
        
        appointmentCount++;
        if (appointmentCount >= 3000) break;
    }
    
    if (appointmentCount >= 3000) break;
}

try {
    db.appointments.insertMany(appointmentsData);
    print(`inserting initial appointments - ok (${appointmentsData.length} records)`);
} catch (e) {
    print(`inserting initial appointments - failed (${e.message})`);
}

print("\nbase structure initialization - ok");


print("\n" + "=".repeat(80));
print("SECTION 2: EXTENDED DATA");
print("=".repeat(80));

print("\ngenerating certificates...");
const certificateTypes = ['sick_leave', 'health', 'vaccination', 'driver', 'pool', 'work', 'education', 'dispensary'];
const certificateTypeWeights = [0.30, 0.25, 0.10, 0.08, 0.07, 0.10, 0.05, 0.05];
const certificates = [];
let certCounter = 10000000;
const certCount = randomInt(800, 1200);

for (let i = 0; i < certCount; i++) {
    const patient = randomItem(patients);
    const doctor = randomItem(doctors);
    const issueDate = randomDate(START_DATE, END_DATE);
    const typeIndex = weightedRandom(certificateTypeWeights);
    const certType = certificateTypes[typeIndex];
    const certificateNumber = `C${String(certCounter++).padStart(8, '0')}`;
    
    let validFrom = issueDate;
    let validUntil = null;
    let content = '';
    let purpose = null;
    let diagnosisId = null;
    
    switch(certType) {
        case 'sick_leave':
            const sickDays = randomInt(3, 14);
            validUntil = new Date(issueDate.getTime() + sickDays * 24 * 60 * 60 * 1000);
            if (diagnoses.length > 0) diagnosisId = randomItem(diagnoses)._id;
            content = `Тимчасово непрацездатний з ${validFrom.toLocaleDateString('uk-UA')} по ${validUntil.toLocaleDateString('uk-UA')} включно. Термін: ${sickDays} днів.`;
            purpose = 'Надання за місцем роботи';
            break;
        case 'health':
            validFrom = issueDate;
            validUntil = new Date(issueDate.getTime() + randomInt(30, 180) * 24 * 60 * 60 * 1000);
            content = `Довідка про стан здоров'я. За даними медичного обстеження стан здоров'я задовільний. Придатний до виконання робіт. Хронічні захворювання: відсутні.`;
            purpose = randomItem(['Надання за місцем роботи', 'Для оформлення документів', 'За місцем вимоги']);
            break;
        case 'vaccination':
            validFrom = issueDate;
            validUntil = new Date(issueDate.getTime() + 365 * 24 * 60 * 60 * 1000);
            content = `Довідка про проведені щеплення. Щеплення: COVID-19, Грип. Медичних протипоказань до щеплень немає.`;
            purpose = 'Для оформлення медичної документації';
            break;
        case 'driver':
            validFrom = issueDate;
            validUntil = new Date(issueDate.getTime() + randomInt(365, 730) * 24 * 60 * 60 * 1000);
            content = `Медична довідка для водіїв. За результатами медичного огляду придатний до керування транспортними засобами категорій B, C. Протипоказань не виявлено.`;
            purpose = 'Для подання до сервісного центру МВС';
            break;
        case 'pool':
            validFrom = issueDate;
            validUntil = new Date(issueDate.getTime() + randomInt(90, 180) * 24 * 60 * 60 * 1000);
            content = `Довідка для відвідування басейну. Медичний огляд пройдено. Протипоказань для відвідування басейну немає. Шкірні захворювання відсутні.`;
            purpose = 'Для відвідування басейну';
            break;
        case 'work':
            validFrom = issueDate;
            validUntil = new Date(issueDate.getTime() + randomInt(180, 365) * 24 * 60 * 60 * 1000);
            content = `Довідка про стан здоров'я для працевлаштування. Профілактичний медичний огляд пройдено. До роботи придатний. Інфекційних захворювань не виявлено.`;
            purpose = 'Для працевлаштування';
            break;
        case 'education':
            validFrom = issueDate;
            validUntil = new Date(issueDate.getTime() + 365 * 24 * 60 * 60 * 1000);
            content = `Довідка про стан здоров'я для навчального закладу. За результатами медичного огляду здоровий. До навчання придатний. Група здоров'я: основна.`;
            purpose = 'Для подання до навчального закладу';
            break;
        case 'dispensary':
            validFrom = issueDate;
            validUntil = new Date(issueDate.getTime() + 365 * 24 * 60 * 60 * 1000);
            content = `Диспансерна довідка. Пройдено диспансерне спостереження. Стан здоров'я на момент огляду: компенсований. Рекомендовано: спостереження у профільного спеціаліста.`;
            purpose = 'Для диспансерного обліку';
            if (Math.random() > 0.5 && diagnoses.length > 0) diagnosisId = randomItem(diagnoses)._id;
            break;
    }
    
    const createdBy = randomItem(users)._id;
    
    certificates.push({
        certificate_number: certificateNumber,
        type: certType,
        patient_id: patient._id,
        doctor_id: doctor._id,
        issue_date: issueDate,
        valid_from: validFrom,
        valid_until: validUntil,
        diagnosis_id: diagnosisId,
        content: content,
        purpose: purpose,
        created_by: createdBy
    });
}

try {
    db.certificates.insertMany(certificates);
    print(`inserting certificates - ok (${certificates.length} records)`);
} catch (e) {
    print(`inserting certificates - failed (${e.message})`);
}

print("\ngenerating multi-doctor appointments...");
const frequentPatients = patients.slice(0, 100);
const additionalAppointments = [];

for (const patient of frequentPatients) {
    const weekStart = randomDate(START_DATE, new Date(END_DATE.getTime() - 7 * 24 * 60 * 60 * 1000));
    const appointmentCount = randomInt(3, 5);
    const usedDoctors = new Set();
    
    for (let i = 0; i < appointmentCount; i++) {
        let doctor;
        let attempts = 0;
        do {
            doctor = randomItem(doctors);
            attempts++;
        } while (usedDoctors.has(doctor._id.toString()) && attempts < 20);
        
        if (usedDoctors.has(doctor._id.toString())) continue;
        usedDoctors.add(doctor._id.toString());
        
        const appointmentDate = new Date(weekStart.getTime() + i * 24 * 60 * 60 * 1000);
        if (appointmentDate.getDay() === 0 || appointmentDate.getDay() === 6) continue;
        
        const schedule = db.schedules.findOne({ entity_id: doctor._id, day_of_week: appointmentDate.getDay() });
        if (!schedule) continue;
        
        const hour = schedule.shift === 'first' ? randomInt(8, 13) : randomInt(14, 19);
        const appointmentTime = formatTime(hour, randomInt(0, 3) * 15);
        const createdBy = randomItem(users)._id;
        
        additionalAppointments.push({
            patient_id: patient._id,
            doctor_id: doctor._id,
            room_id: schedule.room_id,
            appointment_date: appointmentDate,
            appointment_time: appointmentTime,
            type: 'secondary',
            status: 'completed',
            complaints: randomItem(['Консультація спеціаліста', 'Повторний огляд', 'Направлення від сімейного лікаря', 'Контрольний огляд']),
            created_date: new Date(appointmentDate.getTime() - randomInt(1, 7) * 24 * 60 * 60 * 1000),
            created_by: createdBy
        });
    }
}

try {
    db.appointments.insertMany(additionalAppointments);
    print(`inserting multi-doctor appointments - ok (${additionalAppointments.length} records)`);
} catch (e) {
    print(`inserting multi-doctor appointments - failed (${e.message})`);
}

print("\ngenerating angina examinations...");
const anginaDiagnosis = db.diagnoses.findOne({ icd_code: "J03.9" });
const anginaExaminations = [];

if (anginaDiagnosis) {
    const lastMonthStart = new Date(END_DATE.getTime() - 30 * 24 * 60 * 60 * 1000);
    
    for (let i = 0; i < randomInt(50, 80); i++) {
        const patient = randomItem(patients);
        const doctor = randomItem(doctors.filter(d => {
            const spec = db.specialties.findOne({ _id: d.specialty_id });
            return spec && (spec.is_therapist || spec.code === 'ENT');
        }));
        
        if (!doctor) continue;
        
        const examinationDate = randomDate(lastMonthStart, END_DATE);
        let appointment = db.appointments.findOne({
            patient_id: patient._id,
            doctor_id: doctor._id,
            appointment_date: examinationDate,
            status: 'completed'
        });
        
        if (!appointment) {
            const hour = randomInt(8, 18);
            const createdBy = randomItem(users)._id;
            const newAppointment = {
                patient_id: patient._id,
                doctor_id: doctor._id,
                room_id: doctor.room_id,
                appointment_date: examinationDate,
                appointment_time: formatTime(hour, 0),
                type: 'primary',
                status: 'completed',
                complaints: 'Біль у горлі, підвищена температура',
                created_date: new Date(examinationDate.getTime() - 24 * 60 * 60 * 1000),
                created_by: createdBy
            };
            const result = db.appointments.insertOne(newAppointment);
            appointment = { _id: result.insertedId, ...newAppointment };
        }
        
        anginaExaminations.push({
            appointment_id: appointment._id,
            patient_id: patient._id,
            doctor_id: doctor._id,
            examination_date: examinationDate,
            anamnesis: `Скарги на біль у горлі при ковтанні, підвищення температури до 38-39°C протягом ${randomInt(2, 5)} днів. Загальна слабкість.`,
            objective_status: `Стан середньої тяжкості. Температура 38.${randomInt(2, 8)}°C. Зів гіперемований. Мигдалики гіпертрофовані, гіперемовані, з гнійними нашаруваннями. Регіонарні лімфовузли збільшені, болючі.`,
            diagnosis_ids: [anginaDiagnosis._id],
            recommendations: 'Ліжковий режим. Тепле питво. Полоскання горла антисептичними розчинами. Антибіотикотерапія. Повторний огляд через 5 днів.',
            sick_leave_from: examinationDate,
            sick_leave_to: new Date(examinationDate.getTime() + randomInt(5, 10) * 24 * 60 * 60 * 1000),
            follow_up_date: new Date(examinationDate.getTime() + 7 * 24 * 60 * 60 * 1000)
        });
    }
    
    if (anginaExaminations.length > 0) {
        try {
            db.examinations.insertMany(anginaExaminations);
            print(`inserting angina examinations - ok (${anginaExaminations.length} records)`);
        } catch (e) {
            print(`inserting angina examinations - failed (${e.message})`);
        }
    }
} else {
    print("generating angina examinations - skipped (diagnosis not found)");
}

print("\ngenerating patient procedures...");
const activeProcedures = db.procedures.find({ is_active: true }).toArray();
const lastWeekStart = new Date(END_DATE.getTime() - 7 * 24 * 60 * 60 * 1000);
const weeklyProcedures = [];

for (let i = 0; i < randomInt(300, 500); i++) {
    const patient = randomItem(patients);
    const procedure = randomItem(activeProcedures);
    const doctor = randomItem(doctors);
    const prescribedDate = randomDate(new Date(lastWeekStart.getTime() - 7 * 24 * 60 * 60 * 1000), lastWeekStart);
    const scheduledDate = randomDate(lastWeekStart, END_DATE);
    const room = db.rooms.findOne({ room_type: procedure.room_type_required, is_active: true });
    
    weeklyProcedures.push({
        patient_id: patient._id,
        procedure_id: procedure._id,
        prescribed_by: doctor._id,
        prescribed_date: prescribedDate,
        performed_by: doctor._id,
        performed_date: scheduledDate,
        room_id: room ? room._id : null,
        status: 'completed',
        results: 'Процедуру виконано згідно протоколу',
        notes: 'Пацієнт переніс добре'
    });
}

try {
    db.patient_procedures.insertMany(weeklyProcedures);
    print(`inserting patient procedures - ok (${weeklyProcedures.length} records)`);
} catch (e) {
    print(`inserting patient procedures - failed (${e.message})`);
}

print("\ngenerating fluorography procedures...");
const fluoProcedure = db.procedures.findOne({ procedure_code: "D008" });
if (fluoProcedure) {
    const fluoPatientProcedures = [];
    
    for (let i = 0; i < randomInt(200, 300); i++) {
        const patient = randomItem(patients);
        const doctor = randomItem(doctors);
        const prescribedDate = randomDate(START_DATE, END_DATE);
        const scheduledDate = new Date(prescribedDate.getTime() + randomInt(1, 14) * 24 * 60 * 60 * 1000);
        const room = db.rooms.findOne({ room_type: 'diagnostic', is_active: true });
        
        let status;
        let performedDate = null;
        let performedBy = null;
        
        const now = new Date();
        if (scheduledDate < now) {
            status = randomItem(['completed', 'completed', 'completed', 'completed', 'cancelled']);
            if (status === 'completed') {
                performedDate = scheduledDate;
                performedBy = doctor._id;
            }
        } else {
            status = 'scheduled';
        }
        
        fluoPatientProcedures.push({
            patient_id: patient._id,
            procedure_id: fluoProcedure._id,
            prescribed_by: doctor._id,
            prescribed_date: prescribedDate,
            performed_by: performedBy,
            performed_date: performedDate,
            room_id: room ? room._id : null,
            status: status,
            results: status === 'completed' ? randomItem(['Легеневі поля без патологічних змін', 'Легеневий малюнок посилений', 'Без патології', 'Рекомендовано контрольне обстеження']) : null,
            notes: 'Профілактична флюорографія'
        });
    }
    
    try {
        db.patient_procedures.insertMany(fluoPatientProcedures);
        print(`inserting fluorography procedures - ok (${fluoPatientProcedures.length} records)`);
    } catch (e) {
        print(`inserting fluorography procedures - failed (${e.message})`);
    }
} else {
    print("generating fluorography procedures - skipped (procedure not found)");
}


print("\ngenerating vaccinations...");

const vaccinations = [];
const vaccines = [
    { name: 'Грип (Vaxigrip Tetra)', doses: 1 },
    { name: 'COVID-19 (Pfizer-BioNTech)', doses: 2 },
    { name: 'COVID-19 (Moderna)', doses: 2 },
    { name: 'ДТП (Дифтерія-Правець-Поліомієліт)', doses: 1 },
    { name: 'Кір-Краснуха-Паротит (MMR II)', doses: 1 },
    { name: 'Гепатит B', doses: 3 },
    { name: 'Пневмококова інфекція (Prevenar 13)', doses: 1 }
];

const vaccineLots = {
    'Грип (Vaxigrip Tetra)': ['FL2024-10A', 'FL2024-10B', 'FL2024-11A', 'FL2024-11B'],
    'COVID-19 (Pfizer-BioNTech)': ['PF2024-08C', 'PF2024-09A', 'PF2024-09B', 'PF2024-10A'],
    'COVID-19 (Moderna)': ['MOD2024-07D', 'MOD2024-08A', 'MOD2024-09C'],
    'ДТП (Дифтерія-Правець-Поліомієліт)': ['DTP2024-06B', 'DTP2024-07A', 'DTP2024-08C'],
    'Кір-Краснуха-Паротит (MMR II)': ['MMR2024-05A', 'MMR2024-06B', 'MMR2024-07A'],
    'Гепатит B': ['HBV2024-08A', 'HBV2024-09B', 'HBV2024-10A'],
    'Пневмококова інфекція (Prevenar 13)': ['PCV2024-07C', 'PCV2024-08A', 'PCV2024-09B']
};

print("  generating completed vaccinations...");
const completedVaccinationsCount = randomInt(400, 600);

for (let i = 0; i < completedVaccinationsCount; i++) {
    const patient = randomItem(patients);
    const vaccine = randomItem(vaccines);
    const doseNumber = randomInt(1, vaccine.doses);
    
    const scheduledDate = randomDate(START_DATE, new Date());
    const administeredDate = new Date(scheduledDate.getTime() + randomInt(0, 2) * 24 * 60 * 60 * 1000); 
    
    const doctor = randomItem(doctors);
    
    const lot = randomItem(vaccineLots[vaccine.name]);
    
    vaccinations.push({
        patient_id: patient._id,
        vaccine_name: vaccine.name,
        vaccine_lot: lot,
        scheduled_date: scheduledDate,
        administered_date: administeredDate,
        administered_by: doctor._id,
        status: 'completed',
        dose_number: doseNumber,
        notes: doseNumber > 1 ? `Доза ${doseNumber} з ${vaccine.doses}` : null
    });
}

print("  generating scheduled vaccinations...");
const scheduledVaccinationsCount = randomInt(100, 150);

for (let i = 0; i < scheduledVaccinationsCount; i++) {
    const patient = randomItem(patients);
    const vaccine = randomItem(vaccines);
    const doseNumber = randomInt(1, vaccine.doses);
    
    const scheduledDate = randomDate(new Date(), END_DATE); 
    
    const lot = randomItem(vaccineLots[vaccine.name]);
    
    vaccinations.push({
        patient_id: patient._id,
        vaccine_name: vaccine.name,
        vaccine_lot: lot,
        scheduled_date: scheduledDate,
        administered_date: null,
        administered_by: null,
        status: 'scheduled',
        dose_number: doseNumber,
        notes: doseNumber > 1 ? `Заплановано дозу ${doseNumber} з ${vaccine.doses}` : null
    });
}

print("  generating missed vaccinations...");
const missedVaccinationsCount = randomInt(80, 120);

for (let i = 0; i < missedVaccinationsCount; i++) {
    const patient = randomItem(patients);
    const vaccine = randomItem(vaccines);
    const doseNumber = randomInt(1, vaccine.doses);
    
    const scheduledDate = randomDate(START_DATE, new Date()); 
    
    const lot = randomItem(vaccineLots[vaccine.name]);
    
    vaccinations.push({
        patient_id: patient._id,
        vaccine_name: vaccine.name,
        vaccine_lot: lot, 
        scheduled_date: scheduledDate,
        administered_date: null,
        administered_by: null,
        status: 'missed',
        dose_number: doseNumber,
        notes: randomItem([
            'Пацієнт не з\'явився на прийом',
            'Відмінено пацієнтом',
            'Пропущено планове щеплення',
            'Потребує перепризначення',
            'Пацієнт не з\'явився без попередження'
        ])
    });
}

print("  generating contraindicated vaccinations...");
const contraindicatedVaccinationsCount = randomInt(30, 50);

for (let i = 0; i < contraindicatedVaccinationsCount; i++) {
    const patient = randomItem(patients);
    const vaccine = randomItem(vaccines);
    const doseNumber = randomInt(1, vaccine.doses);
    
    const scheduledDate = randomDate(START_DATE, new Date());
    const examinationDate = scheduledDate; 
    
    const doctor = randomItem(doctors);
    
    const lot = randomItem(vaccineLots[vaccine.name]);
    
    vaccinations.push({
        patient_id: patient._id,
        vaccine_name: vaccine.name,
        vaccine_lot: lot,
        scheduled_date: scheduledDate,
        administered_date: null,
        administered_by: null,
        status: 'contraindicated',
        dose_number: doseNumber,
        notes: randomItem([
            `Протипоказання: гостре захворювання. Огляд лікаря від ${examinationDate.toLocaleDateString('uk-UA')}`,
            `Протипоказання: підвищена температура. Перенесено`,
            `Протипоказання: алергічна реакція в анамнезі`,
            `Протипоказання: хронічне захворювання в стадії загострення`,
            `Медвідвід: вагітність`
        ])
    });
}

if (vaccinations.length > 0) {
    try {
        db.vaccinations.insertMany(vaccinations);
        print(`inserting vaccinations - ok (${vaccinations.length} records)`);
        
        const completed = vaccinations.filter(v => v.status === 'completed').length;
        const scheduled = vaccinations.filter(v => v.status === 'scheduled').length;
        const missed = vaccinations.filter(v => v.status === 'missed').length;
        const contraindicated = vaccinations.filter(v => v.status === 'contraindicated').length;
        
        print(`\n  vaccinations by status:`);
        print(`    completed       : ${completed} (${((completed/vaccinations.length)*100).toFixed(1)}%)`);
        print(`    scheduled       : ${scheduled} (${((scheduled/vaccinations.length)*100).toFixed(1)}%)`);
        print(`    missed          : ${missed} (${((missed/vaccinations.length)*100).toFixed(1)}%)`);
        print(`    contraindicated : ${contraindicated} (${((contraindicated/vaccinations.length)*100).toFixed(1)}%)`);
        
        print(`\n  vaccinations by vaccine:`);
        vaccines.forEach(vaccine => {
            const count = vaccinations.filter(v => v.vaccine_name === vaccine.name).length;
            print(`    ${vaccine.name.padEnd(45)}: ${count}`);
        });
        
    } catch (e) {
        print(`inserting vaccinations - failed (${e.message})`);
    }
}

print("\ncreating indexes for vaccinations...");
try {
    db.vaccinations.createIndex({ patient_id: 1, scheduled_date: 1 });
    db.vaccinations.createIndex({ vaccine_name: 1, status: 1 });
    db.vaccinations.createIndex({ scheduled_date: 1, status: 1 });
    db.vaccinations.createIndex({ administered_by: 1, administered_date: 1 });
    db.vaccinations.createIndex({ status: 1 });
    print("creating indexes - ok");
} catch (e) {
    print(`creating indexes - failed (${e.message})`);
}

print("\nvaccinations generation - ok");


print("\n" + "=".repeat(80));
print("SECTION 3: DETAILED MEDICAL EXAMINATIONS");
print("=".repeat(80));

const complaintsByCategory = {
    respiratory: ["сухий кашель протягом 5 днів", "кашель з виділенням жовтуватого мокротиння", "задишка при фізичному навантаженні", "біль у горлі, утруднене ковтання", "закладеність носа, виділення з носа", "підвищення температури до 38.5°C"],
    cardiovascular: ["біль у ділянці серця колючого характеру", "періодичні перебої в роботі серця", "підвищення артеріального тиску до 160/100", "набряки нижніх кінцівок наприкінці дня", "серцебиття в спокої", "біль за грудиною при ходьбі"],
    gastrointestinal: ["біль у епігастрії після прийому їжі", "нудота, відрижка кислим", "здуття живота, метеоризм", "порушення стілу у вигляді запорів", "діарея протягом 3 днів", "важкість у правому підребер'ї"],
    musculoskeletal: ["біль у попереку", "біль у колінних суглобах при навантаженні", "обмеження рухів у шийному відділі", "біль у плечовому суглобі при підняті руки", "ранкова скутість у суглобах", "біль у м'язах після фізичного навантаження"],
    neurological: ["головний біль пульсуючого характеру", "запаморочення при зміні положення тіла", "порушення сну, безсоння", "зниження пам'яті та концентрації уваги", "оніміння пальців рук", "шум у вухах"],
    endocrine: ["спрага, часте сечовипускання", "збільшення маси тіла за останні місяці", "підвищена втомлюваність, слабкість", "тремор рук", "підвищена пітливість", "сухість шкіри"],
    dermatological: ["висип на шкірі з свербінням", "почервоніння та лущення шкіри", "поява нових утворень на шкірі", "сухість та тріщини на шкірі", "випадіння волосся", "зміна кольору нігтів"],
    urogenital: ["біль при сечовипусканні", "часте сечовипускання малими порціями", "біль внизу живота", "порушення менструального циклу", "ниючий біль у поперековій ділянці", "помутніння сечі"],
    general: ["загальна слабкість", "підвищена втомлюваність", "зниження працездатності", "субфебрильна температура", "зниження апетиту", "погіршення загального самопочуття"]
};

const medications = {
    antibiotics: ["Амоксицилін", "Азитроміцин", "Цефтріаксон", "Ципрофлоксацин", "Кларитроміцин"],
    antipyretics: ["Парацетамол", "Ібупрофен", "Німесулід"],
    antihypertensives: ["Еналаприл", "Лозартан", "Амлодипін", "Бісопролол", "Індапамід"],
    cardiac: ["Ацетилсаліцилова кислота", "Клопідогрель", "Аторвастатин", "Метопролол"],
    gastrointestinal: ["Омепразол", "Мотиліум", "Мезим", "Смекта", "Лактулоза"],
    respiratory: ["Бромгексин", "Амброксол", "Беродуал", "Пульмікорт"],
    analgesics: ["Диклофенак", "Кетопрофен", "Мелоксикам", "Парацетамол"],
    vitamins: ["Вітамін D3", "Вітамін B12", "Фолієва кислота", "Магній B6"]
};

function generateVitalSigns(category = 'normal') {
    const vitals = { temp: randomFloat(36.3, 36.9, 1), hr: randomInt(60, 80), rr: randomInt(16, 20), bp: `${randomInt(110, 130)}/${randomInt(70, 85)}` };
    if (category === 'fever') {
        vitals.temp = randomFloat(37.5, 39.5, 1);
        vitals.hr = randomInt(85, 110);
        vitals.rr = randomInt(20, 24);
    } else if (category === 'hypertension') {
        vitals.bp = `${randomInt(140, 170)}/${randomInt(90, 110)}`;
        vitals.hr = randomInt(75, 95);
    } else if (category === 'tachycardia') {
        vitals.hr = randomInt(95, 120);
        vitals.rr = randomInt(20, 26);
    }
    return vitals;
}

function generateAnamnesis(appointmentType, complaints) {
    const templates = {
        acute: ["Захворів гостро {days} днів тому. Початок пов'язує з переохолодженням. {complaints}. Самостійно приймав {medication}.", "Скарги з'явилися {days} днів тому. Відмічає {complaints}. До лікаря не звертався, лікувався самостійно.", "Хворіє протягом {days} днів. Стан погіршився вчора. {complaints}. Алергологічний анамнез не обтяжений."],
        chronic: ["Страждає на {disease} протягом {years} років. Періодично приймає {medication}. Останнє загострення {months} місяців тому.", "В анамнезі {disease}. Знаходиться на диспансерному обліку. Регулярно приймає призначену терапію.", "Хронічне захворювання {disease} протягом {years} років. Останній огляд спеціаліста {months} місяців тому."],
        followup: ["Повторний візит після лікування. Відмічає {improvement}. Скарги зменшились.", "Контрольний огляд. Лікування проводилось амбулаторно. Стан {condition}.", "Явка на контроль після курсу терапії. {complaints}. Рекомендації попереднього візиту виконує."],
        checkup: ["Профілактичний огляд. Хронічні захворювання заперечує. Скарг на момент огляду не пред'являє.", "Диспансеризація. В анамнезі {disease}. Самопочуття задовільне.", "Плановий огляд у зв'язку з {reason}. Скарг активно не пред'являє."]
    };
    
    let category;
    if (appointmentType === 'primary') category = Math.random() > 0.5 ? 'acute' : 'chronic';
    else if (appointmentType === 'secondary') category = 'followup';
    else if (appointmentType === 'checkup') category = 'checkup';
    else category = randomItem(['acute', 'chronic']);
    
    let template = randomItem(templates[category]);
    
    const replacements = {
        '{days}': randomInt(2, 14),
        '{years}': randomInt(2, 15),
        '{months}': randomInt(1, 6),
        '{complaints}': complaints || randomItem(Object.values(complaintsByCategory).flat()),
        '{medication}': randomItem(Object.values(medications).flat()),
        '{disease}': randomItem(['артеріальна гіпертензія', 'цукровий діабет 2 типу', 'ГЕРХ', 'остеохондроз хребта']),
        '{improvement}': randomItem(['поліпшення стану', 'зменшення больового синдрому', 'нормалізацію показників']),
        '{condition}': randomItem(['задовільний', 'стабільний', 'із позитивною динамікою']),
        '{reason}': randomItem(['професійним оглядом', 'диспансеризацією', 'оформленням документів'])
    };
    
    Object.keys(replacements).forEach(key => {
        template = template.replace(new RegExp(key, 'g'), replacements[key]);
    });
    
    return template;
}

function generateObjectiveStatus(diagnosisCategory = 'normal') {
    const vitals = generateVitalSigns(diagnosisCategory === 'respiratory' ? 'fever' : diagnosisCategory);
    
    const normalTemplates = [
        `Загальний стан задовільний. Свідомість ясна, орієнтований у час та просторі. Шкірні покриви та видимі слизові звичайного забарвлення, чисті. Периферічні лімфовузли не збільшені. ЧД ${vitals.rr}/хв. Дихання везикулярне, хрипів немає. Серцева діяльність ритмічна. ЧСС ${vitals.hr}/хв. АТ ${vitals.bp} мм рт.ст. Живіт м'який, безболісний при пальпації. Печінка не збільшена. Симптом Пастернацького негативний з обох боків.`,
        `Стан задовільний. Шкірні покриви нормального кольору та вологості. Температура тіла ${vitals.temp}°C. Органи дихання: дихання везикулярне по всіх легеневих полях. ЧД ${vitals.rr}/хв. Серцево-судинна система: тони серця ясні, ритмічні. ЧСС ${vitals.hr}/хв, пульс ${vitals.hr}/хв, ритмічний. АТ ${vitals.bp} мм рт.ст. Живіт при пальпації м'який, безболісний.`
    ];
    
    const specificTemplates = {
        respiratory: `Стан середньої тяжкості. Шкірні покриви блідорожеві. Температура ${vitals.temp}°C. ЧД ${vitals.rr}/хв. При аускультації легень дихання жорстке, вислуховуються ${randomItem(['розсіяні сухі', 'вологі дрібнопухирчасті', 'поодинокі сухі'])} хрипи. ЧСС ${vitals.hr}/хв. АТ ${vitals.bp} мм рт.ст. Живіт м'який, безболісний.`,
        cardiovascular: `Стан ${randomItem(['задовільний', 'середньої тяжкості'])}. Шкірні покриви блідорожеві. Пульс ${vitals.hr}/хв, ${randomItem(['ритмічний', 'аритмічний'])}. АТ ${vitals.bp} мм рт.ст. При аускультації серця: тони ${randomItem(['приглушені', 'ясні'])}, ритм ${randomItem(['правильний', 'неправильний'])}. Набряки ${randomItem(['відсутні', 'нижніх кінцівок помірні', 'гомілок до середньої третини'])}.`,
        gastrointestinal: `Стан ${randomItem(['задовільний', 'середньої тяжкості'])}. Язик ${randomItem(['вологий, чистий', 'обкладений білим нальотом', 'вологий, рожевий'])}. Живіт ${randomItem(['м\'який', 'помірно напружений', 'здутий'])}, бере участь в акті дихання. При пальпації ${randomItem(['безболісний у всіх відділах', 'болючий у епігастрії', 'помірно болючий у правому підребер\'ї'])}. Печінка ${randomItem(['не збільшена', 'по краю реберної дуги', 'виступає з-під краю реберної дуги на 1-2 см'])}. Селезінка не пальпується. Перистальтика ${randomItem(['збережена', 'ослаблена', 'активна'])}.`
    };
    
    if (specificTemplates[diagnosisCategory] && Math.random() > 0.5) {
        return specificTemplates[diagnosisCategory];
    }
    
    return randomItem(normalTemplates);
}

function generateRecommendations(diagnosisCategory) {
    const recommendations = [];
    
    if (Math.random() > 0.2) {
        const medCategory = diagnosisCategory === 'cardiovascular' ? 'cardiac' :
                           diagnosisCategory === 'respiratory' ? 'respiratory' :
                           diagnosisCategory === 'gastrointestinal' ? 'gastrointestinal' :
                           randomItem(Object.keys(medications));
        
        const drug = randomItem(medications[medCategory]);
        recommendations.push(`Призначено: ${drug} по ${randomItem(['500 мг', '1 таб', '2 таб', '5 мг', '10 мг', '20 мг'])} ${randomItem(['1 раз на день', '2 рази на день', '3 рази на день'])} протягом ${randomInt(5, 14)} днів.`);
    }
    
    if (Math.random() > 0.4) {
        recommendations.push(randomItem(['Дієта: стіл №' + randomInt(1, 15), 'Режим: уникати фізичних навантажень, достатній сон 8-9 годин', 'Дотримуватися питного режиму до 2 літрів на добу', 'Виключити алкоголь, куріння', 'Помірна фізична активність, лікувальна фізкультура']));
    }
    
    if (Math.random() > 0.3) {
        recommendations.push(`Повторна консультація через ${randomInt(7, 21)} днів.`);
    }
    
    return recommendations.join(' ');
}

function determineDiagnosisCategory(diagnosis) {
    const icdCode = diagnosis.icd_code;
    if (icdCode.startsWith('J')) return 'respiratory';
    if (icdCode.startsWith('I')) return 'cardiovascular';
    if (icdCode.startsWith('K')) return 'gastrointestinal';
    if (icdCode.startsWith('M')) return 'musculoskeletal';
    if (icdCode.startsWith('G')) return 'neurological';
    if (icdCode.startsWith('E')) return 'endocrine';
    if (icdCode.startsWith('L')) return 'dermatological';
    if (icdCode.startsWith('N')) return 'urogenital';
    return 'normal';
}

print("\ngenerating detailed examinations...");
const appointments = db.appointments.find({ status: 'completed' }).toArray();
print(`found completed appointments - ok (${appointments.length} records)`);

const appointmentsNeedingExam = appointments.filter(app => !db.examinations.findOne({ appointment_id: app._id }));
print(`appointments needing examination - ok (${appointmentsNeedingExam.length} records)`);

if (appointmentsNeedingExam.length === 0) {
    print("generating examinations - skipped (all appointments have examinations)");
} else {
    const newExaminations = [];
    
    for (const app of appointmentsNeedingExam) {
        const diagnosisCount = app.type === 'checkup' ? 1 : randomInt(1, 3);
        const selectedDiagnoses = [];
        
        for (let i = 0; i < diagnosisCount; i++) {
            const diagnosis = randomItem(diagnoses);
            if (!selectedDiagnoses.find(d => d._id.toString() === diagnosis._id.toString())) {
                selectedDiagnoses.push(diagnosis);
            }
        }
        
        const primaryDiagnosis = selectedDiagnoses[0];
        const diagnosisCategory = determineDiagnosisCategory(primaryDiagnosis);
        
        const anamnesis = generateAnamnesis(app.type, app.complaints);
        const objectiveStatus = generateObjectiveStatus(diagnosisCategory);
        const recommendations = generateRecommendations(diagnosisCategory);
        
        let sickLeaveFrom = null;
        let sickLeaveTo = null;
        const sickLeaveProbability = diagnosisCategory === 'respiratory' ? 0.85 : 
                                     diagnosisCategory === 'gastrointestinal' ? 0.88 :
                                     diagnosisCategory === 'cardiovascular' ? 0.92 : 0.90;
        
        if (Math.random() > sickLeaveProbability && app.type !== 'checkup') {
            sickLeaveFrom = app.appointment_date;
            const duration = diagnosisCategory === 'respiratory' ? randomInt(5, 10) :
                            diagnosisCategory === 'cardiovascular' ? randomInt(7, 14) :
                            randomInt(3, 10);
            sickLeaveTo = new Date(app.appointment_date.getTime() + duration * 24 * 60 * 60 * 1000);
        }
        
        let followUpDate = null;
        const followUpProbability = app.type === 'checkup' ? 0.80 : 
                                    app.type === 'secondary' ? 0.65 : 0.60;
        
        if (Math.random() > followUpProbability) {
            const followUpDays = diagnosisCategory === 'cardiovascular' ? randomInt(7, 14) :
                                diagnosisCategory === 'endocrine' ? randomInt(14, 30) :
                                randomInt(7, 21);
            followUpDate = new Date(app.appointment_date.getTime() + followUpDays * 24 * 60 * 60 * 1000);
        }
        
        newExaminations.push({
            appointment_id: app._id,
            patient_id: app.patient_id,
            doctor_id: app.doctor_id,
            examination_date: app.appointment_date,
            anamnesis: anamnesis,
            objective_status: objectiveStatus,
            diagnosis_ids: selectedDiagnoses.map(d => d._id),
            recommendations: recommendations,
            sick_leave_from: sickLeaveFrom,
            sick_leave_to: sickLeaveTo,
            follow_up_date: followUpDate
        });
    }
    
    if (newExaminations.length > 0) {
        try {
            const result = db.examinations.insertMany(newExaminations, { ordered: false });
            const insertedCount = result.insertedIds ? Object.keys(result.insertedIds).length : newExaminations.length;
            print(`inserting detailed examinations - ok (${insertedCount} records)`);
            print(`  with sick leave: ${newExaminations.filter(e => e.sick_leave_from).length}`);
            print(`  with follow-up: ${newExaminations.filter(e => e.follow_up_date).length}`);
            print(`  avg diagnoses per exam: ${(newExaminations.reduce((sum, e) => sum + e.diagnosis_ids.length, 0) / newExaminations.length).toFixed(1)}`);
        } catch (error) {
            print(`inserting detailed examinations - failed (${error.message})`);
            if (error.writeErrors) {
                print(`  partial insert: ${error.result.nInserted} of ${newExaminations.length}`);
            }
        }
    }
}

print("\ndetailed medical examinations - ok");


print("\ncreating indexes for examinations...");
try {
    db.examinations.createIndex({ appointment_id: 1 }, { unique: true });
    db.examinations.createIndex({ patient_id: 1, examination_date: 1 });
    db.examinations.createIndex({ doctor_id: 1, examination_date: 1 });
    db.examinations.createIndex({ "diagnosis_ids": 1 });
    db.examinations.createIndex({ sick_leave_from: 1, sick_leave_to: 1 });
    print("creating indexes - ok");
} catch (e) {
    print(`creating indexes - failed (${e.message})`);
}

print("\ndetailed medical examinations - ok");

print("\n" + "=".repeat(80));
print("SECTION 4: HOME VISITS (ВИКЛИКИ ДОДОМУ)");
print("=".repeat(80));

print("\ngenerating home visits...");

const homeVisits = [];
const urgencyLevels = ['regular', 'urgent', 'emergency'];
const urgencyWeights = [0.60, 0.30, 0.10]; 
const statuses = ['new', 'assigned', 'in_progress', 'completed', 'cancelled'];

const homeVisitsCount = randomInt(300, 500);

for (let i = 0; i < homeVisitsCount; i++) {
    const patient = randomItem(patients);
    const callDate = randomDate(START_DATE, END_DATE);
    const callHour = randomInt(6, 22);
    const callMinute = randomInt(0, 59);
    const callTime = formatTime(callHour, callMinute);
    
    const urgencyIndex = weightedRandom(urgencyWeights);
    const urgency = urgencyLevels[urgencyIndex];
    
    const now = new Date();
    let status;
    let assignedDoctor = null;
    let visitDate = null;
    let visitTimeSlot = null;
    let examinationId = null;
    
    if (callDate < now) {
        const statusWeights = urgency === 'emergency' ? [0.0, 0.0, 0.0, 0.95, 0.05] :
                            urgency === 'urgent' ? [0.0, 0.0, 0.05, 0.90, 0.05] :
                            [0.0, 0.0, 0.10, 0.85, 0.05];
        const statusIndex = weightedRandom(statusWeights);
        status = statuses[statusIndex];
        
        if (status === 'completed' || status === 'in_progress') {
            const districtDoctors = doctors.filter(d => {
                const spec = db.specialties.findOne({ _id: d.specialty_id });
                return spec && spec.is_therapist;
            });
            
            if (districtDoctors.length > 0) {
                assignedDoctor = randomItem(districtDoctors)._id;
                
                if (urgency === 'emergency') {
                    visitDate = callDate; 
                    visitTimeSlot = 'within_2_hours';
                } else if (urgency === 'urgent') {
                    visitDate = new Date(callDate.getTime() + randomInt(0, 1) * 24 * 60 * 60 * 1000);
                    visitTimeSlot = randomItem(['morning', 'afternoon', 'evening']);
                } else {
                    visitDate = new Date(callDate.getTime() + randomInt(0, 3) * 24 * 60 * 60 * 1000);
                    visitTimeSlot = randomItem(['morning', 'afternoon']);
                }
                
                if (status === 'completed') {
                    const homeVisitAppointment = {
                        patient_id: patient._id,
                        doctor_id: assignedDoctor,
                        room_id: null, 
                        appointment_date: visitDate,
                        appointment_time: visitTimeSlot === 'morning' ? '09:00' : 
                                        visitTimeSlot === 'afternoon' ? '14:00' : 
                                        visitTimeSlot === 'evening' ? '18:00' : '10:00',
                        type: 'primary',
                        status: 'completed',
                        complaints: randomItem([
                            'Виклик додому: підвищення температури',
                            'Виклик додому: біль у животі',
                            'Виклик додому: задишка',
                            'Виклик додому: загальна слабкість',
                            'Виклик додому: загострення хронічного захворювання'
                        ]),
                        created_date: new Date(callDate.getTime() - 60 * 60 * 1000), 
                        created_by: randomItem(users)._id
                    };
                    
                    const appointmentResult = db.appointments.insertOne(homeVisitAppointment);
                    const appointmentId = appointmentResult.insertedId;
                    
                    const examination = {
                        appointment_id: appointmentId, 
                        patient_id: patient._id,
                        doctor_id: assignedDoctor,
                        examination_date: visitDate,
                        anamnesis: `Виклик додому. ${randomItem([
                            'Підвищення температури до 38-39°C, загальна слабкість. Хворіє 2-3 дні.',
                            'Біль у животі, нудота. Скарги з\'явилися сьогодні вранці.',
                            'Задишка при навантаженні, слабкість. Погіршення стану протягом доби.',
                            'Головний біль, запаморочення. Підвищення АТ.',
                            'Загострення хронічного бронхіту. Посилення кашлю з мокротинням.',
                            'Біль у спині, обмеження рухів. Стан після падіння.',
                            'Підвищення температури, біль у горлі. Початок ГРВІ.'
                        ])}`,
                        objective_status: generateObjectiveStatus('normal'),
                        diagnosis_ids: [randomItem(diagnoses)._id],
                        recommendations: generateRecommendations('normal'),
                        sick_leave_from: Math.random() > 0.4 ? visitDate : null,
                        sick_leave_to: Math.random() > 0.4 ? new Date(visitDate.getTime() + randomInt(3, 7) * 24 * 60 * 60 * 1000) : null,
                        follow_up_date: Math.random() > 0.6 ? new Date(visitDate.getTime() + randomInt(3, 7) * 24 * 60 * 60 * 1000) : null
                    };
                    
                    const examResult = db.examinations.insertOne(examination);
                    examinationId = examResult.insertedId;
                }
            }
        }
    } else {
        status = randomItem(['new', 'assigned']);
        
        if (status === 'assigned') {
            const districtDoctors = doctors.filter(d => {
                const spec = db.specialties.findOne({ _id: d.specialty_id });
                return spec && spec.is_therapist;
            });
            
            if (districtDoctors.length > 0) {
                assignedDoctor = randomItem(districtDoctors)._id;
                visitDate = new Date(callDate.getTime() + randomInt(0, 2) * 24 * 60 * 60 * 1000);
                visitTimeSlot = randomItem(['morning', 'afternoon', 'evening']);
            }
        }
    }
    
    const address = `${patient.address.city}, ${patient.address.street}, буд. ${patient.address.building}` +
                   (patient.address.apartment ? `, кв. ${patient.address.apartment}` : '');
    
    const symptoms = randomItem([
        'Висока температура, кашель',
        'Біль у животі, нудота',
        'Задишка, загальна слабкість',
        'Підвищений артеріальний тиск',
        'Головний біль, запаморочення',
        'Загострення хронічного захворювання',
        'Біль у спині, обмеження рухів',
        'Загальна слабкість, втрата апетиту',
        'Біль у грудях',
        'Підвищена температура'
    ]);
    
    const receivedBy = randomItem(users)._id; 
    
    homeVisits.push({
        patient_id: patient._id,
        patient_name: patient.full_name,
        address: address,
        phone: patient.phone,
        alternative_phone: patient.alternative_phone,
        call_date: callDate,
        call_time: callTime,
        urgency: urgency,
        symptoms: symptoms,
        assigned_doctor_id: assignedDoctor,
        visit_date: visitDate,
        visit_time_slot: visitTimeSlot,
        status: status,
        status_updated: callDate,
        received_by: receivedBy,
        notes: status === 'cancelled' ? randomItem(['Пацієнт відмовився', 'Пацієнт не відкрив двері', 'Виклик помилковий']) : null,
        examination_id: examinationId
    });
}

if (homeVisits.length > 0) {
    try {
        db.home_visits.insertMany(homeVisits);
        print(`inserting home visits - ok (${homeVisits.length} records)`);
        
        const completedVisits = homeVisits.filter(v => v.status === 'completed').length;
        const urgentVisits = homeVisits.filter(v => v.urgency === 'urgent' || v.urgency === 'emergency').length;
        print(`  completed visits: ${completedVisits}`);
        print(`  urgent/emergency: ${urgentVisits}`);
    } catch (e) {
        print(`inserting home visits - failed (${e.message})`);
    }
}

print("\ncreating indexes for home_visits...");
try {
    db.home_visits.createIndex({ patient_id: 1, call_date: 1 });
    db.home_visits.createIndex({ assigned_doctor_id: 1, visit_date: 1 });
    db.home_visits.createIndex({ call_date: 1, urgency: 1 });
    db.home_visits.createIndex({ status: 1, call_date: 1 });
    db.home_visits.createIndex({ received_by: 1 });
    db.home_visits.createIndex({ examination_id: 1 });
    print("creating indexes - ok");
} catch (e) {
    print(`creating indexes - failed (${e.message})`);
}

print("\nhome visits generation - ok");


db.guest_requests.createIndex({ email: 1 }, { unique: true });
db.guest_requests.createIndex({ request_code: 1 }, { unique: true });
db.guest_requests.createIndex({ status: 1 });
db.guest_requests.createIndex({ request_date: -1 });
print("\nguest_requests indexes - ok");


print("\n" + "=".repeat(80));
print("SECTION 5: FINAL STATISTICS AND VALIDATION");
print("=".repeat(80));

print("\nvalidating data integrity...");

const validationResults = {
    appointments_without_patients: db.appointments.countDocuments({ patient_id: { $nin: patients.map(p => p._id) } }),
    appointments_without_doctors: db.appointments.countDocuments({ doctor_id: { $nin: doctors.map(d => d._id) } }),
    examinations_without_appointments: db.examinations.countDocuments({ appointment_id: { $ne: null, $nin: db.appointments.find().toArray().map(a => a._id) } }),
    certificates_without_patients: db.certificates.countDocuments({ patient_id: { $nin: patients.map(p => p._id) } }),
    certificates_without_doctors: db.certificates.countDocuments({ doctor_id: { $nin: doctors.map(d => d._id) } }),
    home_visits_without_patients: db.home_visits.countDocuments({ patient_id: { $ne: null, $nin: patients.map(p => p._id) } })
};

print("\ndata integrity check:");
Object.keys(validationResults).forEach(key => {
    const value = validationResults[key];
    const status = value === 0 ? "✓" : "✗";
    print(`  ${status} ${key}: ${value}`);
});

print("\n" + "=".repeat(80));
print("FINAL DATABASE STATISTICS");
print("=".repeat(80));

const finalStats = {
    users: db.users.countDocuments(),
    specialties: db.specialties.countDocuments(),
    rooms: db.rooms.countDocuments(),
    doctors: db.doctors.countDocuments(),
    patients: db.patients.countDocuments(),
    diagnoses: db.diagnoses.countDocuments(),
    procedures: db.procedures.countDocuments(),
    schedules: db.schedules.countDocuments(),
    appointments: db.appointments.countDocuments(),
    examinations: db.examinations.countDocuments(),
    certificates: db.certificates.countDocuments(),
    patient_procedures: db.patient_procedures.countDocuments(),
    vaccinations: db.vaccinations.countDocuments(),
    home_visits: db.home_visits.countDocuments()
};

print("\nCollection counts:");
Object.keys(finalStats).forEach(key => {
    print(`  ${key.padEnd(20)} : ${finalStats[key]}`);
});

print("\n" + "=".repeat(80));
print("DATABASE INITIALIZATION COMPLETED SUCCESSFULLY!");
print("=".repeat(80));

print(`\nTotal execution time: ${(Date.now() - START_DATE.getTime()) / 1000}s`);