// Módulo de la página de canchas: los datos iniciales llegan serializados desde la vista
// Razor (canchasIniciales, horariosCatalogo); este script renderiza el listado y los modales
// de alta/edición, estado y horarios ofrecidos, y llama a los endpoints de CanchaController
// vía apiFetch.
(() => {
    let canchas = Array.isArray(canchasIniciales) ? canchasIniciales : [];
    const horarios = (Array.isArray(horariosCatalogo) ? horariosCatalogo : [])
        .filter(h => h.activo)
        .slice()
        .sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
    let idParaEliminar = null;

    const listaEl = document.getElementById('listaCanchas');
    const inputBuscar = document.getElementById('inputBuscarCancha');
    const selectFiltroEstado = document.getElementById('selectFiltroEstado');

    const modalForm = document.getElementById('modalFormCancha');
    const modalFormTitulo = document.getElementById('modalFormCanchaTitulo');
    const formCancha = document.getElementById('formCancha');
    const formError = document.getElementById('formCanchaError');
    const campoId = document.getElementById('canchaId');
    const campoDescripcion = document.getElementById('campoDescripcion');
    const horariosChecklist = document.getElementById('horariosChecklist');

    function formatearHora(horaStr) {
        return (horaStr || '').substring(0, 5);
    }

    function renderizarChecklistHorarios(seleccionados) {
        const idsSeleccionados = new Set(seleccionados || []);
        horariosChecklist.innerHTML = horarios.map(h => `
            <label class="horario-check">
                <input type="checkbox" value="${h.cod_Horario}" ${idsSeleccionados.has(h.cod_Horario) ? 'checked' : ''}>
                <span>${formatearHora(h.horaInicio)} - ${formatearHora(h.horaFin)}</span>
            </label>`
        ).join('');
    }

    function horariosSeleccionadosDelForm() {
        return Array.from(horariosChecklist.querySelectorAll('input[type="checkbox"]:checked'))
            .map(input => parseInt(input.value, 10));
    }

    const modalBaja = document.getElementById('modalBajaCancha');
    const bajaError = document.getElementById('bajaCanchaError');

    const toast = document.getElementById('toastCancha');

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
        div.textContent = texto;
        return div.innerHTML;
    }

    // ---------- Render ----------

    function renderizar() {
        const texto = inputBuscar.value.trim().toLowerCase();
        const estado = selectFiltroEstado.value; // '', 'Disponible', 'Mantenimiento', 'Baja'

        const filtrados = canchas.filter(c => {
            const coincideTexto = !texto
                || c.nombre.toLowerCase().includes(texto)
                || c.descripcion.toLowerCase().includes(texto);
            const coincideEstado = !estado || c.estado === estado;
            return coincideTexto && coincideEstado;
        });

        if (filtrados.length === 0) {
            listaEl.innerHTML = '<div class="canchas-vacio">No se encontraron canchas.</div>';
            return;
        }

        listaEl.innerHTML = filtrados.map(c => {
            const claseEstado = `cancha-card__estado--${c.estado.toLowerCase()}`;
            const esBaja = c.estado === 'Baja';
            const opciones = ['Disponible', 'Mantenimiento', 'Baja']
                .map(op => `<option value="${op}" ${op === c.estado ? 'selected' : ''}>${op === 'Mantenimiento' ? 'En Mantenimiento' : op}</option>`)
                .join('');

            return `
                <div class="cancha-card" data-id="${c.cod_Cancha}">
                    <div class="cancha-card__icono">⚽</div>
                    <div class="cancha-card__info">
                        <div class="cancha-card__nombre">${escapeHtml(c.nombre)}</div>
                        <div class="cancha-card__descripcion">${escapeHtml(c.descripcion)}</div>
                    </div>
                    <select class="cancha-card__estado ${claseEstado}" data-accion="cambiar-estado" data-id="${c.cod_Cancha}">
                        ${opciones}
                    </select>
                    <div class="cancha-card__acciones">
                        <button type="button" class="btn-editar" data-accion="editar" data-id="${c.cod_Cancha}">✏️ Editar</button>
                        ${esBaja ? '' : `<button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${c.cod_Cancha}" title="Dar de baja">✕</button>`}
                    </div>
                </div>`;
        }).join('');
    }

    // ---------- Modal Alta / Edición ----------

    function abrirModalAlta() {
        modalFormTitulo.textContent = 'Nueva Cancha';
        campoId.value = '';
        formCancha.reset();
        formError.textContent = '';
        renderizarChecklistHorarios([]);
        modalForm.hidden = false;
        campoDescripcion.focus();
    }

    function abrirModalEdicion(id) {
        const cancha = canchas.find(c => c.cod_Cancha === id);
        if (!cancha) return;

        modalFormTitulo.textContent = 'Editar Cancha';
        campoId.value = cancha.cod_Cancha;
        campoDescripcion.value = cancha.descripcion;
        formError.textContent = '';
        renderizarChecklistHorarios(cancha.cod_Horarios);
        modalForm.hidden = false;
        campoDescripcion.focus();
    }

    function cerrarModalForm() {
        modalForm.hidden = true;
    }

    async function guardarCancha(evento) {
        evento.preventDefault();
        formError.textContent = '';

        const descripcion = campoDescripcion.value.trim();
        const codHorarios = horariosSeleccionadosDelForm();
        const id = campoId.value;

        if (!descripcion) {
            formError.textContent = 'La descripción es obligatoria';
            return;
        }

        try {
            let datos;

            if (id) {
                // Primero los horarios: es el paso que puede rechazar una regla de negocio
                // (bloques usados por reservas pendientes); así un rechazo no deja nada a medias.
                const respuestaHorarios = await apiFetch(`/Cancha/HorariosUpdate/${id}`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ cod_Horarios: codHorarios })
                });
                datos = await respuestaHorarios.json();
                if (!respuestaHorarios.ok) {
                    formError.textContent = datos.mensaje || 'No se pudieron guardar los horarios';
                    return;
                }

                const respuestaEdit = await apiFetch(`/Cancha/Edit/${id}`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ descripcion })
                });
                datos = await respuestaEdit.json();
                if (!respuestaEdit.ok) {
                    formError.textContent = datos.mensaje || 'No se pudo guardar la cancha';
                    return;
                }
            } else {
                const respuesta = await apiFetch('/Cancha/Create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ descripcion, cod_Horarios: codHorarios })
                });
                datos = await respuesta.json();
                if (!respuesta.ok) {
                    formError.textContent = datos.mensaje || 'No se pudo crear la cancha';
                    return;
                }
            }

            if (id) {
                canchas = canchas.map(c => c.cod_Cancha === datos.cod_Cancha ? datos : c);
            } else {
                canchas.push(datos);
            }

            cerrarModalForm();
            renderizar();
            mostrarToast(id ? 'Datos guardados correctamente' : 'Cancha creada correctamente');
        } catch (ex) {
            formError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Cambio rápido de estado ----------

    async function cambiarEstado(id, estado, selectEl) {
        try {
            const respuesta = await apiFetch(`/Cancha/EstadoUpdate/${id}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ estado })
            });

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                mostrarToast(datos.mensaje || 'No se pudo actualizar el estado', true);
                renderizar(); // revierte el select a su valor anterior
                return;
            }

            canchas = canchas.map(c => c.cod_Cancha === datos.cod_Cancha ? datos : c);
            renderizar();
            mostrarToast('Estado actualizado correctamente');
        } catch (ex) {
            mostrarToast('Error de conexión con el servidor', true);
            renderizar();
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
            const respuesta = await apiFetch(`/Cancha/Delete/${idParaEliminar}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                bajaError.textContent = datos.mensaje || 'No se pudo dar de baja la cancha';
                return;
            }

            canchas = canchas.map(c => c.cod_Cancha === idParaEliminar ? { ...c, estado: 'Baja' } : c);
            cerrarModalBaja();
            renderizar();
            mostrarToast('Cancha dada de baja');
        } catch (ex) {
            bajaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Eventos ----------

    document.getElementById('btnAbrirAltaCancha').addEventListener('click', abrirModalAlta);
    document.getElementById('btnCancelarFormCancha').addEventListener('click', cerrarModalForm);
    formCancha.addEventListener('submit', guardarCancha);

    // El error del formulario no debe quedar visible después de que el usuario cambia algo.
    formCancha.addEventListener('input', () => { formError.textContent = ''; });
    formCancha.addEventListener('change', () => { formError.textContent = ''; });

    document.getElementById('btnCancelarBajaCancha').addEventListener('click', cerrarModalBaja);
    document.getElementById('btnConfirmarBajaCancha').addEventListener('click', confirmarBaja);

    listaEl.addEventListener('click', (evento) => {
        const boton = evento.target.closest('button[data-accion]');
        if (!boton) return;

        const id = parseInt(boton.dataset.id, 10);
        if (boton.dataset.accion === 'editar') abrirModalEdicion(id);
        if (boton.dataset.accion === 'eliminar') abrirModalBaja(id);
    });

    listaEl.addEventListener('change', (evento) => {
        const select = evento.target.closest('select[data-accion="cambiar-estado"]');
        if (!select) return;
        cambiarEstado(parseInt(select.dataset.id, 10), select.value, select);
    });

    inputBuscar.addEventListener('input', renderizar);
    selectFiltroEstado.addEventListener('change', renderizar);

    [modalForm, modalBaja].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    renderizar();
})();
