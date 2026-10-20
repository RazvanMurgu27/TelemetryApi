import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";

export default function Dashboard() {
    const navigate = useNavigate();

    //Extragem id-ul utilizatorului exact din memoria browserului
    const userId = localStorage.getItem('userId');
    const token = localStorage.getItem('token');

    //1.definim "memoria" paginii
    const [modules, setModules] = useState([]); //Aici tinem lista de baterii
    const [showForm, setShowForm] = useState(false) //Arata/Ascunde formularul

    //DAtele pentru bateria noua
    const [bmsName, setBmsName] = useState('');
    const [deviceId, setDeviceId] = useState('');
    const [cellCount, setCellCount] = useState(4);

    //Functia care cere bateriile de la serverul C#
    const fetchModules = async () => {
        try {
            const response = await fetch(`https://localhost:7186/api/bms/user/${userId}`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            //Daca token-ul e expirat/invalid(401)
            if (response.status === 401) {
                alert("Sesiunea a expirat. Te rugam sa te loghezi din nou!");
                handleLogout();
                return;
            }

            if (response.ok) {
                const data = await response.json();
                console.log("Id-ul meu este:", userId);
                console.log("Bateriile primite de la C# sunt:", data);
                setModules(data);
            }
        }
        catch (error) {
            console.error("Eroare la conectarea cu serverul:", error);
        }
    };

    //Ruleaza la deschiderea paginii
    useEffect(() => {
        //Securitate: Daca nu avem un userId in memorie, inseamna ca nu suntem logati
        if (!userId || !token) {
            navigate('/login');
            return;
        }

        //DAca suntem logati, aducem lista de baterii
        fetchModules();
    }, [userId, token, navigate]);

    //Functia care trimite o baterie noua catre C#
    const handleAddBms = async (e) => {
        e.preventDefault();

        const noileDate = {
            name: bmsName,
            deviceId: deviceId,
            cellCount: parseInt(cellCount),
            userId: parseInt(userId)//ne asiguram ca trimitem numar, nu text catre C#
        };

        try {
            const response = await fetch('https://localhost:7186/api/bms', {
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

            if (response.status === 409) {
                alert("Adaugare esuata! Dispozitivul hardware este deja inregistrat!")
            }
            if (response.ok) {
                setShowForm(false);
                setBmsName('');
                setCellCount(4);
                setDeviceId('');
                fetchModules();//Reincarcam grila cu noile date
            }
        } catch (error) {
            console.error("Eroare la salvare:", error);
        }
    };

    //Functia pentru deconectare
    const handleLogout = () => {
        localStorage.removeItem('userId');
        localStorage.removeItem('token');
        navigate('/login');
    };

    const handleDeleteBms = async (deviceId) => {
        const confirmDelete = window.confirm(`Esti sigur ca vrei sa stergi bateria ${deviceId}? Aceasta actiune este ireversibila!`);
        if (!confirmDelete) return;

        const token = localStorage.getItem('token');

        try {
            const response = await fetch(`https://localhost:7186/api/bms/user/${deviceId}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                setModules(prev => prev.filter(b => b.deviceId !== deviceId));

                alert(`Bateria ${deviceId} a fost stearsa cu succes!`);
            }
            else {
                const errorData = await response.json();
                alert(`Eroare: ${errorData.message}`);
            }
        }
        catch (error) {
            console.error("Eroare de retea la stergere:", error);
            alert("A aparut o eroare la comunicarea cu serverul.");
        }
    };

    const StatusIndicator = ({ deviceId, token }) => {
        const [status, setStatus] = useState('loading');

        useEffect(() => {
            const checkStatus = async () => {
                try {
                    const response = await fetch(`https://localhost:7186/api/telemetry/battery/${deviceId}`, {
                        headers: { 'Authorization': `Bearer ${token}` }
                    });

                    if (response.ok) {
                        const data = await response.json();
                        if (data.length > 0) {
                            const isOffline = (new Date() - new Date(data[0].timestamp + "Z")) > 5 * 60 * 1000;
                            setStatus(isOffline ? 'offline' : 'online');
                        }
                        else {
                            setStatus('nodata');
                        }
                    }
                }
                catch (error) {
                    setStatus('nodata');
                }
            };

            checkStatus();
            const interval = setInterval(checkStatus, 30000);
            return () => clearInterval(interval);
        }, [deviceId, token]);

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
            {/* Header/ Bara de sus*/}
            <div className="flex justify-between items-center mb-8">
                <div>
                    <h1 className="text-3xl font-bold text-emerald-400 mb-2">Hub Telemetrie Baterii</h1>
                    <p className="text-slate-400">Monitorizare in timp real pentru bateriile tale</p>
                </div>

                <div className="flex gap-4">
                    {/* Buton Nou pentru Setări */}
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

            {/*Butonul de adaugare BMS */}
            <button
                onClick={() => setShowForm(!showForm)}
                className="bg-emerald-500 hover:bg-emerald-600 text-white font-bold py-3 px-6 rounded-lg transition-colors flex items-center gap-2 mb-8 shadow-lg shadow-emerald-500/20">
                {showForm ? 'Anuleaza adaugarea' : '+ Adauga un BMS nou'}
            </button>

            {/*Formularul de adaugare */}
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
                        <label className="block text-slate-300 mb-2 text-sm font-medium">Tip Sistem (Tensiune Nominală)</label>
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
            {/*Grila cu baterii */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {modules.map((modul) =>
                    <div
                        key={modul.id}
                        onClick={() => navigate(`/bms/${modul.deviceId}`)}
                        className="bg-slate-800 p-6 rounded-lg border border-slate-700 hover:border-emerald-500 transition-colors relative overflow-hidden group">
                        <div className="absolute top-0 left-0 w-1 h-full bg-emerald-500"></div>
                        <h3 className="text-xl font-bold text-white mb-1">{modul.name}</h3>
                        <p className="text-slate-400 font-mono text-sm mb-6">ID: {modul.deviceId}</p>

                        <div className="mb-6">
                            <StatusIndicator deviceId={modul.deviceId} token={token} />
                        </div>

                        <button
                            onClick={(e) => {
                                e.stopPropagation();
                                handleDeleteBms(modul.deviceId);
                            }}
                            className="bg-red-500/10 hover:bg-red-500 hover:text-white text-red-400 border border-red-500/50 px-3 py-1.5 rounded-md text-sm font-semibold transition-all duration-200 z-10"
                        >
                            Sterge
                        </button>
                    </div>)}

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