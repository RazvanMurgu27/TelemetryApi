import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";

export default function BatterySettings() {
    const navigate = useNavigate();
    const userId = localStorage.getItem('userId');
    const token = localStorage.getItem('token');

    const [modules, setModules] = useState([]);
    const [selectedDeviceId, setSelectedDeviceId] = useState('');
    const [statusMessage, setStatusMessage] = useState({ type: '', text: '' });

    const [formData, setFormData] = useState({
        deviceId: '',
        alertEmail: '',
        maxVoltageThreshold: 3.65,
        minVoltageThreshold: 2.50,
        warningVoltageThreshold: 2.80,
        maxTemperatureThreshold: 55.0,
        minTemperatureThreshold: 0.0,
        maxCurrentThreshold: 220.0,
        maxDeltaVThreshold: 0.20
    })

    // Functie pentru deconectare in caz de token expirat
    const handleLogout = () => {
        localStorage.removeItem('userId');
        localStorage.removeItem('token');
        navigate('/login');
    };

    // 1. Aducem lista de baterii ale utilizatorului
    useEffect(() => {
        if (!userId || !token) {
            navigate('/login');
            return;
        }

        const fetchModules = async () => {
            try {
                const response = await fetch(`https://localhost:7186/api/bms/user/${userId}`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                });

                if (response.status === 401) {
                    alert("Sesiunea a expirat. Te rugam sa te loghezi din nou!");
                    handleLogout();
                    return;
                }

                if (response.ok) {
                    const data = await response.json();
                    setModules(data);
                    if (data.length > 0) {
                        setSelectedDeviceId(data[0].deviceId); // Selectam automat prima baterie
                    }
                }
            } catch (error) {
                console.error("Eroare la aducerea bateriilor", error);
            }
        };

        fetchModules();
    }, [userId, token, navigate]);

    // 2. Aducem setarile cand se schimba bateria selectata din dropdown
    useEffect(() => {
        if (!selectedDeviceId) return;

        const fetchSettings = async () => {
            try {
                const response = await fetch(`https://localhost:7186/api/settings/battery/${selectedDeviceId}`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                });

                if (response.ok) {
                    const data = await response.json();
                    setFormData(data);
                    setStatusMessage({ type: '', text: '' });
                }
            } catch (error) {
                console.error("Eroare la aducerea setariloe!", error);
            }
        };

        fetchSettings();
    }, [selectedDeviceId, token]);

    // Gestionarea inputurilor (Aici am reparat bug-ul cu virgula live)
    const handleChange = (e) => {
        let { name, value } = e.target; 
        
        // Transformăm automat orice virgulă în punct, exceptând adresa de email
        if (typeof value === 'string' && name !== 'alertEmail') {
            value = value.replace(',', '.');
        }

        setFormData(prev => ({ 
            ...prev,
            [name]: value
        }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        // START BLOC DE VALIDARE FRONTEND
        if (formData.maxTemperatureThreshold > 75 || formData.maxTemperatureThreshold < 30) {
            setStatusMessage({ type: 'error', text: 'Temperatura maxima admisa trebuie sa fie intre 30°C și 75°C!' });
            return;
        }

        if (formData.minTemperatureThreshold < -20 || formData.minTemperatureThreshold > 15) {
            setStatusMessage({ type: 'error', text: 'Temperatura minima admisa trebuie sa fie intre -20°C și 15°C!' });
            return;
        }

        if (formData.maxVoltageThreshold > 3.85 || formData.maxVoltageThreshold < 3.40) {
            setStatusMessage({ type: 'error', text: 'Tensiunea maxima trebuie sa fie intre 3.40V și 3.85V!' });
            return;
        }
        
        // Validare logică între limite
        if (formData.minVoltageThreshold < 2.00 || formData.minVoltageThreshold >= formData.maxVoltageThreshold) {
            setStatusMessage({ type: 'error', text: 'Tensiunea minima este invalida sau este mai mare/egala cu tensiunea maxima!' });
            return;
        }

        // Pre-avertizarea trebuie să aibă sens: să fie între minim și maxim
        if (formData.warningVoltageThreshold <= formData.minVoltageThreshold || formData.warningVoltageThreshold >= formData.maxVoltageThreshold) {
            setStatusMessage({ type: 'error', text: 'Tensiunea de pre-avertizare trebuie sa fie plasata între cea minima și cea maxima!' });
            return;
        }

        if (formData.maxCurrentThreshold <= 0 || formData.maxCurrentThreshold > 500) {
            setStatusMessage({ type: 'error', text: 'Curentul maxim trebuie sa fie intre 1A și 500A!' });
            return;
        }

        if (formData.maxDeltaVThreshold < 0.05 || formData.maxDeltaVThreshold > 1.00) {
            setStatusMessage({ type: 'error', text: 'Dezechilibrul maxim admis (Delta V) trebuie sa fie intre 0.05V și 1.00V!' });
            return;
        }
        // --- END BLOC DE VALIDARE FRONTEND ---

        setStatusMessage({ type: 'loading', text: 'Se salveaza parametrii...' });

        // Funcție sigură care convertește orice format de text (cu virgulă sau punct) într-un număr valid
        const safeNumber = (val) => Number(val.toString().replace(',', '.')) || 0;

        // Pregătim datele pentru trimitere
        const payload = {
            deviceId: selectedDeviceId,
            alertEmail: formData.alertEmail, 
            maxVoltageThreshold: safeNumber(formData.maxVoltageThreshold),
            minVoltageThreshold: safeNumber(formData.minVoltageThreshold),
            warningVoltageThreshold: safeNumber(formData.warningVoltageThreshold),
            maxTemperatureThreshold: safeNumber(formData.maxTemperatureThreshold),
            minTemperatureThreshold: safeNumber(formData.minTemperatureThreshold),
            maxCurrentThreshold: safeNumber(formData.maxCurrentThreshold),
            maxDeltaVThreshold: safeNumber(formData.maxDeltaVThreshold)
        };

        try {
            const response = await fetch('https://localhost:7186/api/settings', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(payload)
            });

            if (response.status === 401) {
                alert("Sesiunea a expirat. Te rugam sa te loghezi din nou!");
                handleLogout();
                return;
            }
            if (response.ok) {
                setStatusMessage({ type: 'success', text: 'Parametrii au fost salvați cu succes!' });
                setTimeout(() => setStatusMessage({ type: '', text: '' }), 4000);
            } else {
                const errData = await response.json();
                let errorMessage = 'A aparut o eroare la salvare!';

                if (errData.errors) {
                    const firstErrorKey = Object.keys(errData.errors)[0];
                    errorMessage = errData.errors[firstErrorKey][0];
                } else if (errData.message) {
                    errorMessage = errData.message;
                }

                setStatusMessage({ type: 'error', text: errorMessage });
            }
        } catch (error) {
            console.error("Eroare de rețea la salvarea setărilor:", error);
            setStatusMessage({ type: 'error', text: 'Eroare de comunicare cu serverul. Verifică dacă aplicația backend rulează.' });
        }
    };

    return (
        <div className="min-h-screen bg-slate-900 p-8 font-sans">
            {/* Header / Navigare */}
            <div className="flex justify-between items-center mb-8">
                <div>
                    <h1 className="text-3xl font-bold text-emerald-400 mb-2">Parametrii de siguranta</h1>
                    <p className="text-slate-400">Configureaza limitele personalizate pentru fiecare baterie.</p>
                </div>
                <button
                    onClick={() => navigate('/dashboard')}
                    className="text-slate-400 hover:text-white transition-colors px-4 py-2 border border-slate-700 hover:border-slate-500 rounded-lg"
                >
                    &larr; Inapoi la Dashboard
                </button>
            </div>

            <div className="max-w-4xl bg-slate-800 p-6 rounded-lg border border-slate-700 shadow-2xl">

                {/* Selector Baterie */}
                <div className="mb-8 p-4 bg-slate-900/50 rounded-lg border border-slate-700">
                    <label className="block text-emerald-400 mb-2 text-sm font-bold uppercase tracking-wider">Selecteaza sistemul</label>
                    <select
                        value={selectedDeviceId}
                        onChange={(e) => setSelectedDeviceId(e.target.value)}
                        className="w-full md:w-1/2 p-3 rounded-lg bg-slate-900 text-white border border-slate-600 focus:border-emerald-500 focus:outline-none"
                    >
                        {modules.length === 0 && <option value="">Nu ai baterii inregistrate</option>}
                        {modules.map((modul) => (
                            <option key={modul.deviceId} value={modul.deviceId}>
                                {modul.name} (ID: {modul.deviceId})
                            </option>
                        ))}
                    </select>
                </div>

                {selectedDeviceId && (
                    <form onSubmit={handleSubmit}>

                        <div className="mb-6">
                            <label className="block text-slate-300 mb-2 text-sm font-medium">Adresa email pentru alerte critice</label>
                            <input
                                type="email"
                                name="alertEmail"
                                required
                                value={formData.alertEmail}
                                onChange={handleChange}
                                placeholder="ex: alerta@domeniu.ro"
                                className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-emerald-500 focus:outline-none"
                            />
                        </div>

                        {/* Grid pentru parametrii numerici - Toti au acum type="text" si inputMode="decimal" */}
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">

                            <div className="bg-slate-900/30 p-4 rounded-lg border border-slate-700/50">
                                <label className="block text-yellow-400 mb-2 text-sm font-medium">Tensiune Maxima Celula (V)</label>
                                <input type="text" inputMode="decimal" name="maxVoltageThreshold" value={formData.maxVoltageThreshold} onChange={handleChange} placeholder="ex: 3.65 (Min: 3.40 - Max: 3.85)" className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-yellow-500 focus:outline-none" />
                            </div>

                            <div className="bg-slate-900/30 p-4 rounded-lg border border-slate-700/50">
                                <label className="block text-orange-400 mb-2 text-sm font-medium">Tensiune Minima Celula (V)</label>
                                <input type="text" inputMode="decimal" name="minVoltageThreshold" value={formData.minVoltageThreshold} onChange={handleChange} placeholder="ex: 2.50 (Min: 2.00 - Max: 3.30)" className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-orange-500 focus:outline-none" />
                            </div>

                            <div className="bg-slate-900/30 p-4 rounded-lg border border-slate-700/50">
                                <label className="block text-blue-400 mb-2 text-sm font-medium">Pre-avertizare Descarcare (V)</label>
                                <input type="text" inputMode="decimal" name="warningVoltageThreshold" value={formData.warningVoltageThreshold} onChange={handleChange} placeholder="ex: 2.80 (Min: 2.50 - Max: 3.30)" className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-blue-500 focus:outline-none" />
                            </div>

                            <div className="bg-slate-900/30 p-4 rounded-lg border border-slate-700/50">
                                <label className="block text-cyan-400 mb-2 text-sm font-medium">Dezechilibru Maxim Delta V (V)</label>
                                <input type="text" inputMode="decimal" name="maxDeltaVThreshold" value={formData.maxDeltaVThreshold} onChange={handleChange} placeholder="ex: 0.20 (Min: 0.05 - Max: 1.00)" className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-cyan-500 focus:outline-none" />
                            </div>

                            <div className="bg-slate-900/30 p-4 rounded-lg border border-slate-700/50">
                                <label className="block text-red-400 mb-2 text-sm font-medium">Temperatura Maxima (°C)</label>
                                <input type="text" inputMode="decimal" name="maxTemperatureThreshold" value={formData.maxTemperatureThreshold} onChange={handleChange} placeholder="ex: 55 (Min: 30.0 - Max: 75.0)" className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-red-500 focus:outline-none" />
                            </div>

                            <div className="bg-slate-900/30 p-4 rounded-lg border border-slate-700/50">
                                <label className="block text-indigo-400 mb-2 text-sm font-medium">Temperatura Minima (°C)</label>
                                <input type="text" inputMode="decimal" name="minTemperatureThreshold" value={formData.minTemperatureThreshold} onChange={handleChange} placeholder="ex: 0.0 (Min: -20.0 - Max: 15.0)" className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-indigo-500 focus:outline-none" />
                            </div>

                            <div className="bg-slate-900/30 p-4 rounded-lg border border-slate-700/50 md:col-span-2">
                                <label className="block text-fuchsia-400 mb-2 text-sm font-medium">Curent Maxim Admis (A)</label>
                                <input type="text" inputMode="decimal" name="maxCurrentThreshold" value={formData.maxCurrentThreshold} onChange={handleChange} placeholder="ex: 220.0 (Min: 1.0 - Max: 500.0)" className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-fuchsia-500 focus:outline-none" />
                            </div>
                        </div>

                        {/* Mesaje status salvare */}
                        {statusMessage.text && (
                            <div className={`mb-6 p-4 rounded-lg font-semibold ${statusMessage.type === 'success' ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/50' :
                                    statusMessage.type === 'error' ? 'bg-red-500/20 text-red-400 border border-red-500/50' :
                                        'bg-blue-500/20 text-blue-400 border border-blue-500/50'
                                }`}>
                                {statusMessage.text}
                            </div>
                        )}

                        <button type="submit" className="w-full bg-emerald-500 hover:bg-emerald-600 text-white font-bold py-4 rounded-lg transition-colors shadow-lg shadow-emerald-500/20 text-lg">
                            Salveaza Parametrii pentru {selectedDeviceId}
                        </button>
                    </form>
                )}
            </div>
        </div>
    );
}