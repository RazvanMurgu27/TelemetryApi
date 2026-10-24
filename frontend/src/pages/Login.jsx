import { useState } from "react";
import { useNavigate } from "react-router-dom";

/**
 * Componenta responsabila pentru autentificarea si inregistrarea utilizatorilor.
 * Interactioneaza cu API-ul .NET Core pentru validarea credentialelor, emiterea si
 * stocarea in siguranta a token-ului JWT necesar pentru accesarea rutelor protejate.
 * Ofera capabilitati de validare pe client si tratare a erorilor de retea.
 */
export default function Login() {
    const navigate = useNavigate();

    // Managementul starii locale pentru formulare si tratarea erorilor
    const [isLoginMode, setIsLoginMode] = useState(true);
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');

    /**
     * Handler asincron pentru trimiterea datelor catre server.
     * Gestioneaza atat fluxul de inregistrare (Register) cat si cel de autentificare (Login).
     */
    const handleSubmit = async (e) => {
        // Prevenirea comportamentului implicit de reincarcare a paginii la submisia formularului
        e.preventDefault(); 
        
        // Resetarea starii de eroare la fiecare noua incercare de autentificare
        setError(''); 

        // Determinarea dinamica a rutei API in functie de modul activ al componentei
        const API_URL = import.meta.env.VITE_API_URL;
        const endpoint = isLoginMode ? '/api/auth/login' : '/api/auth/register';
        const url = `${API_URL}${endpoint}`;

        try {
            // Configurarea si trimiterea cererii HTTP (POST) conform standardelor REST
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ email: email, password: password })
            });

            // Gestionarea erorilor de validare returnate de server (ex: Cod 400 Bad Request / 401 Unauthorized)
            if (!response.ok) {
                const errorMessage = await response.text();
                setError(errorMessage);
                return;
            }
    
            // Procesarea raspunsului de succes (Status 200 OK)
            if (isLoginMode) {
                const data = await response.json();

                // Stocarea credentialelor de sesiune in memoria persistenta a browserului (Local Storage)
                localStorage.setItem('userId', data.userId);
                localStorage.setItem('token', data.token);

                // Redirectionarea automata catre panoul de control
                navigate('/dashboard');
            } else {
                // Notificarea utilizatorului cu privire la finalizarea inregistrarii
                alert(`Contul a fost creat cu succes pentru adresa: ${email}\n\n ❗️IMPORTANT: Toate alertele de siguranta pentru sistemele tale vor fi trimise pe aceasta adresa de email! Puteti schimba adresa din meniul de setari al fiecarei baterii!`);
                
                // Tranzitia automata inapoi la modul de autentificare dupa o inregistrare cu succes
                setIsLoginMode(true);
                setPassword('');
            }
        } catch (err) {
            // Tratarea exceptiilor critice de retea (Server offline, erori CORS, conexiune refuzata)
            setError('Eroare de conexiune cu serverul! Verifica daca backend-ul ruleaza!');
        }
    };

    return (
        <div className="min-h-screen bg-slate-900 flex items-center justify-center p-4">
            <div className="bg-slate-800 p-8 rounded-2xl shadow-2xl max-w-md w-full border border-slate-700">
                <h1 className="text-3xl font-bold text-emerald-400 mb-6 text-center">
                    {isLoginMode ? 'Autentificare Sistem' : 'Creare Cont Nou'}
                </h1>

                {/* Feedback vizual dinamic in caz de eroare de autentificare/retea */}
                {error && (
                    <div className="bg-red-500/10 border border-red-500/50 text-red-400 p-3 rounded-lg mb-6 text-sm text-center">
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-5">
                    <div>
                        <label className="block text-slate-300 mb-2 text-sm font-medium">Adresa Email</label>
                        <input
                            type="email"
                            required
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="w-full bg-slate-900 text-white rounded-lg p-3 border border-slate-700 focus:border-emerald-500 focus:outline-none"
                            placeholder="nume@exemplu.com"
                        /> 
                    </div>
                    <div>
                        <label className="block text-slate-300 mb-2 text-sm font-medium">Parola</label>
                        <input
                            type="password"
                            required
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="w-full bg-slate-900 text-white rounded-lg p-3 border border-slate-700 focus:border-emerald-500 focus:outline-none"
                            placeholder="********"
                        />
                    </div>

                    <button type="submit" className="w-full bg-emerald-500 hover:bg-emerald-600 text-white font-semibold py-3 rounded-lg transition-colors mt-4 shadow-lg shadow-emerald-500/20">
                        {isLoginMode ? 'Intra in cont' : 'Inregistreaza-te'}
                    </button>
                </form>
                
                {/* Mecanismul de comutare (Toggle) intre modurile de Autentificare si Inregistrare */}
                <div className="mt-6 text-center border-t border-slate-700 pt-6">
                    <p className="text-slate-400 text-sm mb-2">
                        {isLoginMode ? 'Nu ai sistemul configurat inca?' : 'Ai deja un cont activ?'}
                    </p>
                    <button
                        onClick={() => {
                            setIsLoginMode(!isLoginMode);
                            setError(''); // Curatarea oricarei erori existente la schimbarea contextului
                        }}
                        className="text-emerald-400 hover:text-emerald-300 font-semibold text-sm transition-colors">
                        {isLoginMode ? 'Creeaza un cont nou' : 'Inapoi la autentificare'}
                    </button>
                </div>
            </div>
        </div> 
    );
}