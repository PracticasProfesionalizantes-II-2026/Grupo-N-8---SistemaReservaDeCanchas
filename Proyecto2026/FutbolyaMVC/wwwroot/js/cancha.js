(() => {
    let canchas = Array.isArray(canchasIniciales) ? canchasIniciales : [];
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
        const estado = selectFiltroEstado.value; // '', 'disponible', 'mantenimiento'

        const filtrados = canchas.filter(c => {
            const coincideTexto = !texto
                || c.nombre.toLowerCase().includes(texto)
                || c.descripcion.toLowerCase().includes(texto);
            const coincideEstado =
                !estado ||
                (estado === 'disponible' && c.estado) ||
                (estado === 'mantenimiento' && !c.estado);
            return coincideTexto && coincideEstado;
        });

        if (filtrados.length === 0) {
            listaEl.innerHTML = '<div class="canchas-vacio">No se encontraron canchas.</div>';
            return;
        }

        listaEl.innerHTML = filtrados.map(c => {
            const claseEstado = c.estado ? 'cancha-card__estado--disponible' : 'cancha-card__estado--mantenimiento';
            const textoEstado = c.estado ? 'Disponible' : 'En Mantenimiento';
            return `
                <div class="cancha-card" data-id="${c.cod_Cancha}">
                    <div class="cancha-card__icono">⚽</div>
                    <div class="cancha-card__info">
                        <div class="cancha-card__nombre">${escapeHtml(c.nombre)}</div>
                        <div class="cancha-card__descripcion">${escapeHtml(c.descripcion)}</div>
                    </div>
                    <div class="cancha-card__estado ${claseEstado}">${textoEstado}</div>
                    <div class="cancha-card__acciones">
                        <button type="button" class="btn-editar" data-accion="editar" data-id="${c.cod_Cancha}">✏️ Editar</button>
                        <button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${c.cod_Cancha}" title="Eliminar">✕</button>
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
        modalForm.hidden = false;
        campoDescripcion.focus();
    }

    function cerrarModalForm() {
        modalForm.hidden = true;
    }

    async function guardarCancha(evento) {
        evento.preventDefault();
        formError.textContent = '';

        const request = {
            descripcion: campoDescripcion.value.trim()
        };

        const id = campoId.value;
        const url = id ? `/Cancha/Edit/${id}` : '/Cancha/Create';

        try {
            const respuesta = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                formError.textContent = datos.mensaje || 'No se pudo guardar la cancha';
                return;
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
            const respuesta = await fetch(`/Cancha/Delete/${idParaEliminar}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                bajaError.textContent = datos.mensaje || 'No se pudo eliminar la cancha';
                return;
            }

            canchas = canchas.filter(c => c.cod_Cancha !== idParaEliminar);
            cerrarModalBaja();
            renderizar();
            mostrarToast('Cancha eliminada');
        } catch (ex) {
            bajaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Eventos ----------

    document.getElementById('btnAbrirAltaCancha').addEventListener('click', abrirModalAlta);
    document.getElementById('btnCancelarFormCancha').addEventListener('click', cerrarModalForm);
    formCancha.addEventListener('submit', guardarCancha);

    document.getElementById('btnCancelarBajaCancha').addEventListener('click', cerrarModalBaja);
    document.getElementById('btnConfirmarBajaCancha').addEventListener('click', confirmarBaja);

    listaEl.addEventListener('click', (evento) => {
        const boton = evento.target.closest('button[data-accion]');
        if (!boton) return;

        const id = parseInt(boton.dataset.id, 10);
        if (boton.dataset.accion === 'editar') abrirModalEdicion(id);
        if (boton.dataset.accion === 'eliminar') abrirModalBaja(id);
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
