import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";

/**
 * Componenta Smart pentru pagina principala (Dashboard) a platformei IoT.
 * Responsabilitati:
 * 1. Extragerea securizata a listei de dispozitive BMS inregistrate pe contul curent.
 * 2. Gestionarea starii formularului pentru adaugarea unui nou dispozitiv hardware.
 * 3. Afisarea starii curente de conectivitate a fiecarui modul (Online/Offline/Asteptare Date) prin componenta auxiliara StatusIndicator.
 * 4. Navigarea catre vizualizarea de detaliu a telemetriei pentru un modul specific.
 * 5. Gestionarea procesului de autentificare (verificare token si delogare).
 */
export default function Dashboard() {
    const navigate = useNavigate();

    // Extragerea credentialelor de identificare (Token JWT si ID) stocate la autentificare
    const userId = localStorage.getItem('userId');
    const token = localStorage.getItem('token');

    // Managementul starii locale (Local State)
    const [modules, setModules] = useState([]); // Lista modulelor BMS asociate utilizatorului
    const [showForm, setShowForm] = useState(false); // Flag de control pentru randarea conditionala a formularului de inregistrare

    // Starea formularului pentru inregistrarea unui nou dispozitiv
    const [bmsName, setBmsName] = useState('');
    const [deviceId, setDeviceId] = useState('');
    const [cellCount, setCellCount] = useState(4);

    /**
     * Functie asincrona pentru obtinerea listei de dispozitive hardware (BMS Modules).
     * Ruleaza automat la incarcarea componentei si re-ruleaza la adaugarea/stergerea unui modul.
     */
    const fetchModules = async () => {
        try {
            const API_URL = import.meta.env.VITE_API_URL;
            const response = await fetch(`${API_URL}/api/bms/user/${userId}`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            // Tratarea erorilor de autentificare: Sesiune expirata
            if (response.status === 401) {
                alert("Sesiunea a expirat. Te rugam sa te loghezi din nou!");
                handleLogout();
                return;
            }

            if (response.ok) {
                const data = await response.json();
                setModules(data);
            }
        } catch (error) {
            console.error("Eroare la conectarea cu serverul:", error);
        }
    };

    // Hook pentru initierea preluarii datelor la montarea componentei
    useEffect(() => {
        // Guard Clause: Redirectionare automata daca utilizatorul nu este autentificat
        if (!userId || !token) {
            navigate('/login');
            return;
        }

        fetchModules();
    }, [userId, token, navigate]);

    /**
     * Handler pentru submisia formularului de inregistrare a unui nou dispozitiv.
     * Converteste tipurile de date si trimite o cerere POST catre API.
     */
    const handleAddBms = async (e) => {
        e.preventDefault();

        // Modelarea obiectului de date conform specificatiilor endpoint-ului (Data Transfer Object)
        const noileDate = {
            name: bmsName,
            deviceId: deviceId,
            cellCount: parseInt(cellCount),
            userId: parseInt(userId) // Asiguram parsarea corecta la tipul de date asteptat de C#
        };

        try {
            const API_URL = import.meta.env.VITE_API_URL;
            const response = await fetch(`${API_URL}/api/bms`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(noileDate)
            });

            if (response.status === 401) {
                alert("Sesiunea a expirat. Te rugam sa te loghezi din nou!");
                handleLogout();
                return;
            }

            // Gestionarea erorilor de validare a unicitatii (Conflict)
            if (response.status === 409) {
                alert("Adaugare esuata! Dispozitivul hardware este deja inregistrat!");
            }
            if (response.ok) {
                // Resetarea starii formularului in urma operatiunii de adaugare reusite
                setShowForm(false);
                setBmsName('');
                setCellCount(4);
                setDeviceId('');
                fetchModules(); // Actualizarea interfetei grafice cu noul dispozitiv adaugat
            }
        } catch (error) {
            console.error("Eroare la salvare:", error);
        }
    };

    // Handler pentru procedura de delogare a utilizatorului si curatarea mediului de stocare local
    const handleLogout = () => {
        localStorage.removeItem('userId');
        localStorage.removeItem('token');
        navigate('/login');
    };

    /**
     * Handler pentru operatiunea destructiva de stergere a unui modul BMS.
     * Include mecanism de confirmare (Sanity Check) pentru prevenirea stergerilor accidentale.
     * 
     * @param {string} deviceId - Identificatorul unic (MAC / UUID) al dispozitivului ce urmeaza sa fie sters.
     */
    const handleDeleteBms = async (deviceId) => {
        const confirmDelete = window.confirm(`Esti sigur ca vrei sa stergi bateria ${deviceId}? Aceasta actiune este ireversibila!`);
        if (!confirmDelete) return;

        const token = localStorage.getItem('token');

        try {
            const API_URL = import.meta.env.VITE_API_URL;
            const response = await fetch(`${API_URL}/api/bms/user/${deviceId}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                // Actualizarea optimista a listei din interfata grafica pentru a reflecta stergerea (Optimistic UI Update)
                setModules(prev => prev.filter(b => b.deviceId !== deviceId));
                alert(`Bateria ${deviceId} a fost stearsa cu succes!`);
            } else {
                const errorData = await response.json();
                alert(`Eroare: ${errorData.message}`);
            }
        } catch (error) {
            console.error("Eroare de retea la stergere:", error);
            alert("A aparut o eroare la comunicarea cu serverul.");
        }
    };

    /**
     * Sub-componenta React pentru evaluarea in timp real a conectivitatii unui dispozitiv.
     * Implementeaza un mecanism de tip "Watchdog", interogand API-ul la intervale regulate (Polling)
     * pentru a determina daca hardware-ul (ESP32) inca transmite pachete de telemetrie.
     * 
     * Stari de functionare:
     * - 'loading': Stare initiala in timpul primei incarcari de date
     * - 'nodata': Dispozitivul a fost inregistrat, dar nu a receptionat niciun pachet inca
     * - 'offline': Timpul trecut de la primirea ultimului pachet depaseste pragul de siguranta (Timeout)
     * - 'online': Componentele hardware transmit datele normal.
     */
    const StatusIndicator = ({ deviceId, token }) => {
        const [status, setStatus] = useState('loading');

        useEffect(() => {
            const checkStatus = async () => {
                try {
                    const API_URL = import.meta.env.VITE_API_URL;
                    // Preluarea exclusiva a ultimei inregistrari logate (Log Record)
                    const response = await fetch(`${API_URL}/api/telemetry/battery/${deviceId}`, {
                        headers: { 'Authorization': `Bearer ${token}` }
                    });

                    if (response.ok) {
                        const data = await response.json();
                        if (data.length > 0) {
                            // Implementarea logicii Dead Man's Switch (5 minute timeout) pentru stabilirea statusului
                            const isOffline = (new Date() - new Date(data[0].timestamp + "Z")) > 5 * 60 * 1000;
                            setStatus(isOffline ? 'offline' : 'online');
                        } else {
                            setStatus('nodata');
                        }
                    }
                } catch (error) {
                    setStatus('nodata');
                }
            };

            checkStatus(); // Prima evaluare de status la montarea componentei

            // Programarea reevaluarilor la un interval setat de 30 de secunde
            const interval = setInterval(checkStatus, 30000);
            
            // Clean-up function pentru prevenirea problemelor cauzate de memorie la demontarea componentei
            return () => clearInterval(interval);
        }, [deviceId, token]);

        // Tratam randarea conditionata in functie de starea activa
        if (status === 'loading') return <span className="text-slate-500 text-sm">Se verifica starea...</span>;
        if (status === 'nodata') return <span className="text-slate-500 text-sm">Se asteapta date...</span>;
        if (status === 'offline') return (
            <div className="flex items-center gap-2">
                <span className="w-3 h-3 bg-red-500 rounded-full animate-pulse"></span>
                <span className="text-red-400 font-semibold text-sm">OFFLINE (Niciun semnal)</span>
            </div>
        );
        return (
            <div className="flex items-center gap-2">
                <span className="w-3 h-3 bg-green-500 rounded-full animate-pulse"></span>
                <span className="text-green-400 font-semibold text-sm">Monitorizare Activa</span>
            </div>
        );
    };

    return (
        <div className="min-h-screen bg-slate-900 p-8 font-sans">
            {/* Sectiunea Header - Navigare si Control de Cont */}
            <div className="flex justify-between items-center mb-8">
                <div>
                    <h1 className="text-3xl font-bold text-emerald-400 mb-2">Hub Telemetrie Baterii</h1>
                    <p className="text-slate-400">Monitorizare in timp real pentru bateriile tale</p>
                </div>

                <div className="flex gap-4">
                    {/* Buton navigare catre interfata de gestionare a parametrilor de siguranta (BMS Settings) */}
                    <button
                        onClick={() => navigate('/settings')}
                        className="text-emerald-400 hover:text-emerald-300 font-semibold transition-colors px-4 py-2 border border-emerald-500/30 hover:border-emerald-400 rounded-lg bg-emerald-500/10" >
                        ⚙️ Setari Parametrii
                    </button>

                    <button
                        onClick={handleLogout}
                        className="text-slate-400 hover:text-red-400 transition-colors px-4 py-2 border border-slate-700 hover:border-red-400 rounded-lg" >
                        Deconectare
                    </button>
                </div>
            </div>

            {/* Buton central pentru declansarea modului de inregistrare a unui dispozitiv */}
            <button
                onClick={() => setShowForm(!showForm)}
                className="bg-emerald-500 hover:bg-emerald-600 text-white font-bold py-3 px-6 rounded-lg transition-colors flex items-center gap-2 mb-8 shadow-lg shadow-emerald-500/20">
                {showForm ? 'Anuleaza adaugarea' : '+ Adauga un BMS nou'}
            </button>

            {/* Formular de adaugare a unui modul (Inregistrare Device Hardware) */}
            {showForm && (
                <form onSubmit={handleAddBms} className="bg-slate-800 p-6 rounded-lg border border-slate-700 max-w-md mb-8 shadow-2xl">
                    <h2 className="text-xl text-white font-semibold mb-4">Inregistreaza un modul nou</h2>

                    <div className="mb-4">
                        <label className="block text-slate-300 mb-2 text-sm font-medium">Nume Baterie</label>
                        <input
                            type="text"
                            required
                            value={bmsName}
                            onChange={(e) => setBmsName(e.target.value)}
                            placeholder="ex: Sistem Solar Casa"
                            className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-emerald-500 focus:outline-none"
                        />
                    </div>

                    <div className="mb-6">
                        <label className="block text-slate-300 mb-2 text-sm font-medium">Adresa MAC / Device ID</label>
                        <input
                            type="text"
                            required
                            value={deviceId}
                            onChange={(e) => setDeviceId(e.target.value)}
                            placeholder="ex: SIM-001"
                            className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-emerald-500 focus:outline-none"
                        />
                    </div>
                    <div className="mb-6">
                        <label className="block text-slate-300 mb-2 text-sm font-medium">Tip Sistem (Tensiune Nominala)</label>
                        <select
                            value={cellCount}
                            onChange={(e) => setCellCount(e.target.value)}
                            className="w-full p-3 rounded-lg bg-slate-900 text-white border border-slate-700 focus:border-emerald-500 focus:outline-none"
                        >
                            <option value={4}>Sistem 12V (4 Celule LiFePO4)</option>
                            <option value={8}>Sistem 24V (8 Celule LiFePO4)</option>
                            <option value={16}>Sistem 48V (16 Celule LiFePO4)</option>
                        </select>
                    </div>
                    <button type="submit" className="w-full bg-emerald-500 hover:bg-emerald-600 text-white font-bold py-3 rounded-lg transition-colors">
                        Salveaza Bateria
                    </button>
                </form>
            )}
            
            {/* Grila flexibila pentru randarea modulelor BMS stocate in starea componentei */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {modules.map((modul) => (
                    <div
                        key={modul.id}
                        // Navigare la pagina de date detaliate (BatteryDetails) folosind rute dinamice (Dynamic Routing)
                        onClick={() => navigate(`/bms/${modul.deviceId}`)}
                        className="bg-slate-800 p-6 rounded-lg border border-slate-700 hover:border-emerald-500 transition-colors relative overflow-hidden group cursor-pointer"
                    >
                        <div className="absolute top-0 left-0 w-1 h-full bg-emerald-500"></div>
                        <h3 className="text-xl font-bold text-white mb-1">{modul.name}</h3>
                        <p className="text-slate-400 font-mono text-sm mb-6">ID: {modul.deviceId}</p>

                        <div className="mb-6">
                            <StatusIndicator deviceId={modul.deviceId} token={token} />
                        </div>

                        {/* Buton destructiv. Se izoleaza evenimentul de clic (e.stopPropagation) pentru a preveni activarea navigarii onClick-ului componentiei parinte */}
                        <button
                            onClick={(e) => {
                                e.stopPropagation();
                                handleDeleteBms(modul.deviceId);
                            }}
                            className="bg-red-500/10 hover:bg-red-500 hover:text-white text-red-400 border border-red-500/50 px-3 py-1.5 rounded-md text-sm font-semibold transition-all duration-200 z-10"
                        >
                            Sterge
                        </button>
                    </div>
                ))}

                {/* Stare Vida (Empty State): Trateaza scenariul in care lista de dispozitive hardware este nula si formularul e ascuns */}
                {modules.length === 0 && !showForm && (
                    <div className="col-span-full flex flex-col items-center justify-center p-12 border-2 border-dashed border-slate-700 rounded-lg bg-slate-800/50">
                        <p className="text-slate-400 mb-4">Nu ai niciun sistem BMS inregistrat.</p>
                        <button
                            onClick={() => setShowForm(true)}
                            className="text-emerald-400 font-semibold hover:text-emerald-300 transition-colors"
                        >
                            Apasa aici pentru a adauga prima baterie.
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
}