// Módulo de la página de usuarios: los datos iniciales llegan serializados desde la vista
// Razor (usuariosIniciales); este script renderiza el listado y los modales de alta/edición/
// reactivación/reseteo de contraseña, y llama a los endpoints de UsuarioController vía apiFetch.
(() => {
    let usuarios = Array.isArray(usuariosIniciales) ? usuariosIniciales : [];
    let idParaEliminar = null;
    let idParaResetear = null;
    let modoReactivar = false;

    const listaEl = document.getElementById('listaUsuarios');
    const inputBuscar = document.getElementById('inputBuscar');
    const selectFiltroRol = document.getElementById('selectFiltroRol');
    const selectFiltroEstado = document.getElementById('selectFiltroEstado');

    const modalForm = document.getElementById('modalForm');
    const modalFormTitulo = document.getElementById('modalFormTitulo');
    const formUsuario = document.getElementById('formUsuario');
    const formError = document.getElementById('formError');
    const campoId = document.getElementById('usuarioId');
    const campoNombre = document.getElementById('campoNombre');
    const campoApellido = document.getElementById('campoApellido');
    const campoDni = document.getElementById('campoDni');
    const campoDireccion = document.getElementById('campoDireccion');
    const campoCorreo = document.getElementById('campoCorreo');
    const campoContraseña = document.getElementById('campoContraseña');
    const campoRol = document.getElementById('campoRol');
    const grupoContraseña = document.getElementById('grupoContraseña');

    const modalBaja = document.getElementById('modalBaja');
    const bajaError = document.getElementById('bajaError');

    const modalReset = document.getElementById('modalReset');
    const resetError = document.getElementById('resetError');

    const toast = document.getElementById('toast');

    // ---------- Utilidades ----------

    function mostrarToast(mensaje, esError = false) {
        toast.textContent = mensaje;
        toast.classList.toggle('toast--error', esError);
        toast.classList.add('toast--visible');
        clearTimeout(mostrarToast._t);
        mostrarToast._t = setTimeout(() => toast.classList.remove('toast--visible'), 3000);
    }

    function escapeHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto ?? '';
        return div.innerHTML;
    }

    // ---------- Render ----------

    function renderizar() {
        const texto = inputBuscar.value.trim().toLowerCase();
        const rolFiltro = selectFiltroRol.value; // '', 'admin', 'operador'
        const estadoFiltro = selectFiltroEstado.value; // '', 'activo', 'baja'

        const filtrados = usuarios.filter(u => {
            const coincideTexto = !texto
                || `${u.nombre} ${u.apellido} ${u.correo} ${u.dni}`.toLowerCase().includes(texto);
            const coincideRol =
                !rolFiltro ||
                (rolFiltro === 'admin' && u.rol) ||
                (rolFiltro === 'operador' && !u.rol);
            const coincideEstado = !estadoFiltro || (estadoFiltro === 'activo' ? u.activo : !u.activo);
            return coincideTexto && coincideRol && coincideEstado;
        });

        if (filtrados.length === 0) {
            listaEl.innerHTML = '<div class="productos-vacio">No se encontraron usuarios.</div>';
            return;
        }

        listaEl.innerHTML = filtrados.map(u => {
            const claseRol = u.rol ? 'usuario-card__rol--admin' : 'usuario-card__rol--operador';
            const textoRol = u.rol ? 'Administrador' : 'Operador';
            const acciones = u.activo
                ? `<button type="button" class="btn-resetear" data-accion="resetear" data-id="${u.cod_Usuario}" title="Restablecer contraseña">🔑</button>
                   <button type="button" class="btn-editar" data-accion="editar" data-id="${u.cod_Usuario}">✏️ Editar</button>
                   <button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${u.cod_Usuario}" title="Eliminar">✕</button>`
                : `<button type="button" class="btn-reactivar" data-accion="reactivar" data-id="${u.cod_Usuario}">↺ Reactivar</button>`;

            return `
                <div class="producto-card ${u.activo ? '' : 'producto-card--baja'}" data-id="${u.cod_Usuario}">
                    <div class="producto-card__info">
                        <div class="producto-card__nombre">${escapeHtml(u.nombre)} ${escapeHtml(u.apellido)} ${u.activo ? '' : '<span class="badge-baja">Baja</span>'}</div>
                        <div class="producto-card__meta">
                            <span class="usuario-card__correo">${escapeHtml(u.correo)}</span>
                            <span class="usuario-card__rol ${claseRol}">${textoRol}</span>
                        </div>
                    </div>
                    <div class="producto-card__acciones">
                        ${acciones}
                    </div>
                </div>`;
        }).join('');
    }

    // ---------- Modal Alta / Edición ----------

    function abrirModalAlta() {
        modoReactivar = false;
        modalFormTitulo.textContent = 'Nuevo Usuario';
        campoId.value = '';
        formUsuario.reset();
        grupoContraseña.hidden = false;
        campoContraseña.required = true;
        formError.textContent = '';
        modalForm.hidden = false;
        campoNombre.focus();
    }

    function abrirModalEdicion(id) {
        const usuario = usuarios.find(u => u.cod_Usuario === id);
        if (!usuario) return;

        modoReactivar = false;
        modalFormTitulo.textContent = 'Editar Usuario';
        campoId.value = usuario.cod_Usuario;
        campoNombre.value = usuario.nombre;
        campoApellido.value = usuario.apellido;
        campoDni.value = usuario.dni;
        campoDireccion.value = usuario.direccion ?? '';
        campoCorreo.value = usuario.correo;
        campoRol.value = usuario.rol ? 'true' : 'false';

        // La contraseña no se edita desde acá: para eso está "Restablecer contraseña".
        grupoContraseña.hidden = true;
        campoContraseña.required = false;

        formError.textContent = '';
        modalForm.hidden = false;
        campoNombre.focus();
    }

    // Reactivar un usuario dado de baja directamente desde el listado (además del flujo que
    // ofrece reactivar cuando se intenta crear uno con el mismo DNI/correo). Requiere una
    // contraseña temporal nueva, igual que un alta.
    function abrirModalReactivar(id) {
        const usuario = usuarios.find(u => u.cod_Usuario === id);
        if (!usuario) return;

        modoReactivar = true;
        modalFormTitulo.textContent = 'Reactivar Usuario';
        campoId.value = usuario.cod_Usuario;
        campoNombre.value = usuario.nombre;
        campoApellido.value = usuario.apellido;
        campoDni.value = usuario.dni;
        campoDireccion.value = usuario.direccion ?? '';
        campoCorreo.value = usuario.correo;
        campoRol.value = usuario.rol ? 'true' : 'false';

        grupoContraseña.hidden = false;
        campoContraseña.required = true;
        campoContraseña.value = '';

        formError.textContent = '';
        modalForm.hidden = false;
        campoNombre.focus();
    }

    function cerrarModalForm() {
        modalForm.hidden = true;
        modoReactivar = false;
    }

    async function guardarUsuario(evento) {
        evento.preventDefault();
        formError.textContent = '';

        const id = campoId.value;
        const rol = campoRol.value === 'true';
        const necesitaContraseña = !id || modoReactivar;

        const request = necesitaContraseña
            ? {
                nombre: campoNombre.value.trim(),
                apellido: campoApellido.value.trim(),
                dni: campoDni.value.trim(),
                direccion: campoDireccion.value.trim(),
                correo: campoCorreo.value.trim(),
                contraseña: campoContraseña.value,
                rol
            }
            : {
                nombre: campoNombre.value.trim(),
                apellido: campoApellido.value.trim(),
                dni: campoDni.value.trim(),
                direccion: campoDireccion.value.trim(),
                correo: campoCorreo.value.trim(),
                rol
            };

        const url = modoReactivar
            ? `/Usuario/Reactivar/${id}`
            : (id ? `/Usuario/Edit/${id}` : '/Usuario/Create');

        try {
            const respuesta = await apiFetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                // Si el DNI/correo pertenece a un usuario dado de baja, la API lo indica con
                // cod_usuario_inactivo — se ofrece reactivarlo en vez de solo mostrar el error.
                if (!id && datos.cod_usuario_inactivo) {
                    await ofrecerReactivar(datos.cod_usuario_inactivo, request, datos.mensaje);
                    return;
                }

                formError.textContent = datos.mensaje || 'No se pudo guardar el usuario';
                return;
            }

            if (id) {
                usuarios = usuarios.map(u => u.cod_Usuario === datos.cod_Usuario ? datos : u);
            } else {
                usuarios.push(datos);
            }

            const mensajeExito = modoReactivar
                ? 'Usuario reactivado correctamente. Deberá cambiar su contraseña al ingresar.'
                : (id ? 'Datos guardados correctamente' : 'Usuario creado correctamente. Deberá cambiar su contraseña al ingresar.');

            cerrarModalForm();
            renderizar();
            mostrarToast(mensajeExito);
        } catch (ex) {
            formError.textContent = 'Error de conexión con el servidor';
        }
    }

    async function ofrecerReactivar(codUsuarioInactivo, request, mensaje) {
        const confirmar = window.confirm(`${mensaje}\n\nAceptar: reactivarlo con estos datos nuevos.\nCancelar: no hacer nada.`);
        if (!confirmar) {
            formError.textContent = mensaje;
            return;
        }

        try {
            const respuesta = await apiFetch(`/Usuario/Reactivar/${codUsuarioInactivo}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                formError.textContent = datos.mensaje || 'No se pudo reactivar el usuario';
                return;
            }

            usuarios = usuarios.map(u => u.cod_Usuario === datos.cod_Usuario ? datos : u);
            cerrarModalForm();
            renderizar();
            mostrarToast('Usuario reactivado correctamente. Deberá cambiar su contraseña al ingresar.');
        } catch (ex) {
            formError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Modal Baja ----------

    function abrirModalBaja(id) {
        idParaEliminar = id;
        bajaError.textContent = '';
        modalBaja.hidden = false;
    }

    function cerrarModalBaja() {
        modalBaja.hidden = true;
        idParaEliminar = null;
    }

    async function confirmarBaja() {
        if (idParaEliminar == null) return;
        bajaError.textContent = '';

        try {
            const respuesta = await apiFetch(`/Usuario/Delete/${idParaEliminar}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                bajaError.textContent = datos.mensaje || 'No se pudo eliminar el usuario';
                return;
            }

            usuarios = usuarios.filter(u => u.cod_Usuario !== idParaEliminar);
            cerrarModalBaja();
            renderizar();
            mostrarToast('Usuario eliminado');
        } catch (ex) {
            bajaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Modal Reset de contraseña ----------

    function abrirModalReset(id) {
        idParaResetear = id;
        resetError.textContent = '';
        modalReset.hidden = false;
    }

    function cerrarModalReset() {
        modalReset.hidden = true;
        idParaResetear = null;
    }

    async function confirmarReset() {
        if (idParaResetear == null) return;
        resetError.textContent = '';

        try {
            const respuesta = await apiFetch(`/Usuario/ResetearContrasena/${idParaResetear}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                resetError.textContent = datos.mensaje || 'No se pudo enviar el código';
                return;
            }

            cerrarModalReset();
            mostrarToast(datos.mensaje || 'Código enviado correctamente');
        } catch (ex) {
            resetError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Eventos ----------

    document.getElementById('btnAbrirAlta').addEventListener('click', abrirModalAlta);
    document.getElementById('btnCancelarForm').addEventListener('click', cerrarModalForm);
    formUsuario.addEventListener('submit', guardarUsuario);

    // El error del formulario no debe quedar visible después de que el usuario cambia algo.
    formUsuario.addEventListener('input', () => { formError.textContent = ''; });
    formUsuario.addEventListener('change', () => { formError.textContent = ''; });

    document.getElementById('btnCancelarBaja').addEventListener('click', cerrarModalBaja);
    document.getElementById('btnConfirmarBaja').addEventListener('click', confirmarBaja);

    document.getElementById('btnCancelarReset').addEventListener('click', cerrarModalReset);
    document.getElementById('btnConfirmarReset').addEventListener('click', confirmarReset);

    listaEl.addEventListener('click', (evento) => {
        const boton = evento.target.closest('button[data-accion]');
        if (!boton) return;

        const id = parseInt(boton.dataset.id, 10);
        if (boton.dataset.accion === 'editar') abrirModalEdicion(id);
        if (boton.dataset.accion === 'eliminar') abrirModalBaja(id);
        if (boton.dataset.accion === 'resetear') abrirModalReset(id);
        if (boton.dataset.accion === 'reactivar') abrirModalReactivar(id);
    });

    inputBuscar.addEventListener('input', renderizar);
    selectFiltroRol.addEventListener('change', renderizar);
    selectFiltroEstado.addEventListener('change', renderizar);

    [modalForm, modalBaja, modalReset].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    renderizar();
})();
