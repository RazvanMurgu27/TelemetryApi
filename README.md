# IoT Smart Battery Management System (BMS) & Telemetry Platform

Platforma software moderna, orientata pe arhitectura de tip 
microservicii si IoT (Internet of Things), destinata monitorizarii 
in timp real, diagnosticarii si stocarii datelor telemetrice 
provenite de la sisteme de acumulatoare cu celule LiFePO4.

---

## Cuprins
- [1. Despre Proiect & Arhitectura](#1-despre-proiect--arhitectura)
- [2. Tehnologii Utilizate](#2-tehnologii-utilizate)
- [3. Aportul Inovativ si Beneficiile Sistemului](#3-aportul-inovativ-si-beneficiile-sistemului)
- [4. Structura Proiectului](#4-structura-proiectului)
- [5. Ghid de Rulare (Pentru Testare Locala)](#5-ghid-de-rulare-pentru-testare-locala)
- [6. Functionalitati Speciale (Data Seeder & Stress Test)](#6-functionalitati-speciale-data-seeder--stress-test)

---

## 1. Despre Proiect & Arhitectura

Aplicatia asigura o punte sigura si eficienta intre hardware-ul 
fizic (placute microcontroler ESP32 conectate la module BMS prin 
magistrala RS485/Modbus) si un panou de control (Dashboard) modern 
dezvoltat in React.

Fluxul datelor functioneaza pe baza unui model asincron si scalabil:
* **Achizitia de date:** Dispozitivul ESP32 citeste parametrii 
  celulelor de la BMS si ii transmite catre backend.
* **Brokerul de Mesagerie:** Se utilizeaza **RabbitMQ** pentru a 
  prelua pachetele de telemetrie si a decupla primirea lor de 
  procesarea intensiva din baza de date.
* **Backend-ul (ASP.NET Core):** Un Worker Service dedicat 
  (`TelemetryConsumerService`) preia mesajele din coada RabbitMQ, 
  le valideaza si le stocheaza in baza de date.
* **Frontend-ul (React + Vite):** Ofera o interfata grafica 
  interactiva, bazata pe randare client-side, grafice in timp real 
  (Recharts) si alerte vizuale.

---

## 2. Tehnologii Utilizate

### **Backend (.NET Ecosystem)**
* **ASP.NET Core Web API:** Arhitectura RESTful orientata pe servicii.
* **Entity Framework Core (EF Core):** ORM modern pentru maparea OR.
* **SQLite:** Baza de date relationala rapida si portabila.
* **RabbitMQ:** Coada de mesaje asincrona pentru fluxul IoT.
* **JWT & ASP.NET Identity:** Autentificare bazata pe token-uri 
  criptografice si Secret Manager.
* **MailKit / SmtpClient:** Serviciu automatizat de alerte pe email.

### **Frontend (React Ecosystem)**
* **React (Vite):** Librarie de interfete utilizator de inalta performanta.
* **Tailwind CSS:** Framework utilitar pentru un design responsive.
* **Recharts:** Librarie avansata pentru grafice SVG interactive.
* **React Router DOM:** Gestionarea rutelor dinamice in aplicatia SPA.

---

## 3. Aportul Inovativ si Beneficiile Sistemului

* **Siguranta Proactiva (Dead Man's Switch & Thresholds):** 
  Platforma nu doar afiseaza date pasiv, ci evalueaza constant 
  parametrii in raport cu limite configurabile per baterie 
  (delta V, praguri termice) si declanseaza alerte automate.
* **Decuplare Arhitecturala (IoT Ready):** Folosirea RabbitMQ 
  permite backend-ului sa gestioneze mii de cereri simultane de la 
  hardware fara a bloca firul principal de executie.
* **Optimizare Client-Side:** Calculele complexe (cum ar fi 
  diferenta de potential dintre celule - Delta V) sunt procesate 
  eficient in browser pentru a reduce incarcarea pe server.
* **Securitate la Standarde Industriale:** Separarea stricta a 
  secretelor (JWT Keys, credentiale SMTP) prin variabile de mediu 
  si Secret Manager previne scurgerile de date pe GitHub.

---

## 4. Structura Proiectului

```text
├── Backend/               # Proiectul ASP.NET Core API & Worker Service
│   ├── Controllers/       # Endpoints REST (Auth, BMS, Settings, Telemetry)
│   ├── Services/          # Logica de business si Consumerul RabbitMQ
│   ├── Data/              # Contextul bazei de date si DataSeeder
│   └── appsettings.json   # Configuratii generale si conexiuni
│
└── Frontend/              # Aplicatia React (Vite)
    ├── src/
    │   ├── pages/    # Dashboard, BatteryDetails, BatteryChart, Settings, Login
    │   └── App.jsx        # Rutele principale
    ├── .env               # Variabile de mediu (VITE_API_URL)
    └── package.json       # Dependente proiect frontend
```
---

## 5. Ghid de Rulare (Pentru Testare Locala)
* Daca esti nou in acest proiect si vrei sa testezi aplicatia pe calculatorul tau, urmeaza pasii de mai jos:
* Pasul 1: Clonarea Repository-ului
* Deschide terminalul si cloneaza proiectul:

```bash
git clone [https://github.com/RazvanMurgu27/NumeRepo.git](https://github.com/RazvanMurgu27/NumeRepo.git)
cd NumeRepo
```
* Pasul 2: Pornirea Brokerului RabbitMQ
* Asigura-te ca ai instalat Docker si ruleaza containerul RabbitMQ:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```
* (Poti accesa panoul de control in browser la adresa http://localhost:15672 folosind user/parola guest/guest).

* Pasul 3: Configurarea si Rularea Backend-ului
* 1. Mergi in folderul TelemetryApi
* 2. Verifica fisierul appsettings.json sa aiba hostname-ul RabbitMQ setat pe localhost.
* 3. Ruleaza aplicatia C# din terminal sau din IDE-ul preferat (Visual Studio / Visual Studio Code):
```bash
dotnet run
```
* Serverul va porni automat pe portul securizat HTTPS (de obicei https://localhost:7186).
* Odata ce backend-ul este pornit, poti vizualiza si testa toate rutele 
disponibile in mod interactiva accesand in browser:
`https://localhost:7186/swagger` (sau portul indicat in terminalul tau).
Swagger ofera o interfata vizuala directa pentru testarea ruterelor 
de Autentificare, BMS, Setari si Telemetrie.

* Pasul 4: Configurarea si Rularea Frontend-ului
* 1. Deschide un terminal nou si mergi in folderul frontend.
* 2. Creeaza un fisier numit .env in radacina folderului de frontend si adauga linia:

```code snippet
VITE_API_URL=https://localhost:7186
```

* 3. Instaleaza dependentele si porneste aplicatia React:
```bash
npm install
npm run dev
```

* 4. Accesează link-ul afisat in terminal (de ex: http://localhost:5173) in browserul tau.

---

## 6.Functionalitati Speciale(Data Seeder & Stress Test)
* Pentru a simplifica evaluarea si testarea aplicatiei, sistemul include mecanisme dedicate:
Data Seeder: La prima pornire a aplicatiei C#, baza de date SQLite este creata automat, iar aplicatia injecteaza un cont de administrator prestabilit (configurat in appsettings.json) impreuna cu setari implicite de siguranta pentru celule LiFePO4. Acest lucru elimina necesitatea configurarii manuale laborioase inainte de primul test.
---
* BMS Stress Test (Simularea de Telemetrie): Daca doresti sa testezi comportamentul graficelor din React si reactia worker-ului RabbitMQ la un volum mare de date fara a avea fizic placheta ESP32 conectata, poti rula scriptul sau utilitarul de Stress Test integrat in backend. Acesta va genera mii de pachete de telemetrie simulate (cu variații de tensiune, curent, temperaturi si erori injectate), populand cozile RabbitMQ si testand performanta in timp real a dashboard-ului.




