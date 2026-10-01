// Helper único usado por todos los módulos en vez de fetch() directo.
// Agrega el header RequestVerificationToken (antiforgery) leído del <meta> que
// _Layout.cshtml renderiza, para toda escritura (POST/PUT/PATCH/DELETE).
// Si la sesión expiró (401 de la API, revalidado por la API y propagado acá por
// AuthorizedHttpMessageHandler; o 401 propio del controlador MVC cuando la sesión no existe)
// o si el token antiforgery venció tras una inactividad larga (400 sin cuerpo JSON),
// redirige a Login en vez de dejar que el módulo muestre "Error de conexión".
(function (global) {
    function tokenAntifalsificacion() {
        var meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.content : '';
    }

    function esRedirectALogin(respuesta) {
        // fetch() sigue los redirects automáticamente; si la navegación terminó en
        // /Account/Login, algo en el camino (sesión, middleware) nos mandó para allá.
        return respuesta.redirected && /\/Account\/Login/i.test(respuesta.url);
    }

    function irALogin() {
        window.location.href = '/Account/Login?expirada=1';
    }

    // La promesa nunca se resuelve ni se rechaza: el código que llamó a apiFetch (los
    // .then()/.catch() o el await dentro de un try/catch de cada módulo) queda esperando
    // para siempre, sin llegar nunca a mostrar "Error de conexión". Mientras tanto ya
    // disparamos la redirección a Login.
    function promesaColgada() {
        return new Promise(function () {});
    }

    // Mientras una escritura está en curso, una segunda idéntica (mismo método, URL y cuerpo)
    // no se envía: recibe la misma respuesta. Evita ventas/ingresos de stock duplicados (o un
    // correo de reseteo de contraseña duplicado) por un doble clic, que el back no puede
    // distinguir de dos operaciones legítimas.
    var escriturasEnCurso = new Map();

    async function apiFetch(url, options) {
        var opciones = Object.assign({}, options);
        opciones.headers = new Headers(opciones.headers || {});

        var metodo = (opciones.method || 'GET').toUpperCase();
        var esEscritura = metodo !== 'GET' && metodo !== 'HEAD';

        if (!esEscritura) {
            return enviar(url, opciones);
        }

        opciones.headers.set('RequestVerificationToken', tokenAntifalsificacion());

        var clave = metodo + ' ' + url + ' ' + (typeof opciones.body === 'string' ? opciones.body : '');
        if (escriturasEnCurso.has(clave)) {
            // Cada llamador recibe su propia copia: el cuerpo de una Response se lee una sola vez.
            return escriturasEnCurso.get(clave).then(function (r) { return r.clone(); });
        }

        // El botón que disparó la acción se deshabilita hasta que llega la respuesta, así el
        // usuario ve que el clic se tomó. Se restaura su estado previo (puede venir deshabilitado).
        var boton = document.activeElement instanceof HTMLButtonElement ? document.activeElement : null;
        var botonEstabaDeshabilitado = boton ? boton.disabled : false;
        if (boton) boton.disabled = true;

        var promesa = enviar(url, opciones);
        escriturasEnCurso.set(clave, promesa);
        try {
            var respuesta = await promesa;
            return respuesta.clone();
        } finally {
            escriturasEnCurso.delete(clave);
            if (boton) boton.disabled = botonEstabaDeshabilitado;
        }
    }

    async function enviar(url, opciones) {
        var tieneBody = opciones.body != null && !(opciones.body instanceof FormData);
        if (tieneBody && !opciones.headers.has('Content-Type')) {
            opciones.headers.set('Content-Type', 'application/json');
        }

        // Un error de red real (servidor caído, sin conexión) sigue siendo responsabilidad
        // del módulo que llama: se propaga tal cual para que su catch muestre el mensaje.
        var respuesta = await fetch(url, opciones);

        if (respuesta.status === 401 || esRedirectALogin(respuesta)) {
            irALogin();
            return promesaColgada();
        }

        // Un 400 sin cuerpo JSON es casi siempre el middleware de antiforgery (token vencido)
        // devolviendo texto plano, no un error de validación de negocio (esos siempre
        // responden JSON con { mensaje }).
        if (respuesta.status === 400) {
            var contentType = respuesta.headers.get('content-type') || '';
            if (contentType.indexOf('application/json') === -1) {
                irALogin();
                return promesaColgada();
            }
        }

        return respuesta;
    }

    global.apiFetch = apiFetch;
})(window);
