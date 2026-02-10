using Google.Cloud.Firestore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
namespace Blockbuster.API.Services;

public class FirebaseService
{
    /*
     * _firestoreDb: Instancia de la base de datos FS
     * Se guarda como variable privada porque solo este servicio lo maneja
     */
    
    private readonly FirestoreDb _firestoreDb;
    
    /*
     * _logger: Para registrar eventos (errores, informacion)
     * Nos permite ver que esta pasando en la consola / logs
     */
    private readonly ILogger<FirebaseService> _logger;

    /*
     * Constructor: Se ejecuta cuando la app arranca
     * Recibe un ILogger inyectado por ASP.NET Core
     */
    public FirebaseService(ILogger<FirebaseService> logger)
    {
        _logger = logger;

        try
        {
            /*
             * Paso 1: Obtener la ruta del archivo de las cred
             * AppContext.BaseDirectory: Directorio / folder raiz de la app
             * Path.Combine: Une las rutas de forma segura
             */
            var credentialsPath = Path.Combine(
                AppContext.BaseDirectory,
                "Config",
                "firebase-credentials.json"
            );
            
            /*
             * Paso 2: Validar que el archivo existe
             * Si no existe, lanzamos una excepcion para detener la app
             */

            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException(
                    $"Archivo de credenciales no encontrado en: {credentialsPath}"
                    );
            }
            
            /*
             * Paso 3: Inicializar Firebase Admin SDK
             * GoogleCredential.FromFile: Lee y parsea el JSON de las cred
             * FirebaseApp.Create: Registra la app de FB en memoria
             */

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialsPath)
                }
                );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
