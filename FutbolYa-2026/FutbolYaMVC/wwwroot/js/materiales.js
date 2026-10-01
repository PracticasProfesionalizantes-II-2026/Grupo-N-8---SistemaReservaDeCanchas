// Módulo de la página de materiales: los datos iniciales llegan serializados desde la vista
// Razor (materialesIniciales); este script renderiza el listado y los modales de alta/edición/
// ajuste de stock, y llama a los endpoints de MaterialController vía apiFetch.
(() => {
    let materiales = Array.isArray(materialesIniciales) ? materialesIniciales : [];
    let idParaEliminar = null;

    const listaEl = document.getElementById('listaMateriales');
    const inputBuscar = document.getElementById('inputBuscar');
    const selectFiltroEstado = document.getElementById('selectFiltroEstado');

    const modalForm = document.getElementById('modalForm');
    const modalFormTitulo = document.getElementById('modalFormTitulo');
    const formMaterial = document.getElementById('formMaterial');
    const formError = document.getElementById('formError');
    const campoId = document.getElementById('materialId');
    const campoNombre = document.getElementById('campoNombre');
    const grupoCantidad = document.getElementById('grupoCantidad');
    const campoCantidad = document.getElementById('campoCantidad');

    const modalBaja = document.getElementById('modalBaja');
    const bajaError = document.getElementById('bajaError');

    // Modal de ajuste relativo de stock.
    const modalStock = document.getElementById('modalStock');
    const stockActual = document.getElementById('stockActual');
    const stockPreview = document.getElementById('stockPreview');
    const stockError = document.getElementById('stockError');
    const campoAjusteCantidad = document.getElementById('campoAjusteCantidad');
    const btnIngreso = document.getElementById('btnIngreso');
    const btnEgreso = document.getElementById('btnEgreso');
    const btnConfirmarStock = document.getElementById('btnConfirmarStock');
    let idParaAjustarStock = null;
    let tipoAjuste = 'ingreso';

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
        div.textContent = texto;
        return div.innerHTML;
    }

    // ---------- Render ----------

    function renderizar() {
        const texto = inputBuscar.value.trim().toLowerCase();
        const estado = selectFiltroEstado.value; // '', 'activo', 'baja'

        const filtrados = materiales.filter(m => {
            const coincideTexto = !texto || m.nombre.toLowerCase().includes(texto);
            const coincideEstado = !estado || (estado === 'activo' ? m.activo : !m.activo);
            return coincideTexto && coincideEstado;
        });

        if (filtrados.length === 0) {
            listaEl.innerHTML = '<div class="productos-vacio">No se encontraron materiales.</div>';
            return;
        }

        listaEl.innerHTML = filtrados.map(m => {
            const stockBajo = m.cant_Material <= 5;
            const acciones = m.activo
                ? `<button type="button" class="btn-editar" data-accion="editar" data-id="${m.cod_Material}">✏️ Editar</button>
                   <button type="button" class="btn-ajustar-stock" data-accion="ajustar-stock" data-id="${m.cod_Material}">📦 Ajustar stock</button>
                   <button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${m.cod_Material}" title="Eliminar">✕</button>`
                : `<button type="button" class="btn-reactivar" data-accion="reactivar" data-id="${m.cod_Material}">↺ Reactivar</button>`;

            return `
                <div class="producto-card ${m.activo ? '' : 'producto-card--baja'}" data-id="${m.cod_Material}">
                    <div class="producto-card__tipo producto-card__tipo--material">🏐</div>
                    <div class="producto-card__info">
                        <div class="producto-card__nombre">${escapeHtml(m.nombre)} ${m.activo ? '' : '<span class="badge-baja">Baja</span>'}</div>
                        <div class="producto-card__meta">
                            <span class="producto-card__stock ${stockBajo ? 'producto-card__stock--bajo' : ''}">Stock: ${m.cant_Material}</span>
                        </div>
                    </div>
                    <div class="producto-card__acciones">
                        ${acciones}
                    </div>
                </div>`;
        }).join('');
    }

    // ---------- Modal Alta / Edición ----------

    // El mismo modal se reusa para alta/edición; el campo Stock solo se muestra
    // (y es obligatorio) al crear — al editar el stock se ajusta con el modal aparte.
    function abrirModalAlta() {
        modalFormTitulo.textContent = 'Nuevo Material';
        campoId.value = '';
        formMaterial.reset();
        grupoCantidad.hidden = false;
        campoCantidad.required = true;
        formError.textContent = '';
        modalForm.hidden = false;
        campoNombre.focus();
    }

    function abrirModalEdicion(id) {
        const material = materiales.find(m => m.cod_Material === id);
        if (!material) return;

        modalFormTitulo.textContent = 'Editar Material';
        campoId.value = material.cod_Material;
        campoNombre.value = material.nombre;
        grupoCantidad.hidden = true;
        campoCantidad.required = false;
        campoCantidad.value = '';
        formError.textContent = '';
        modalForm.hidden = false;
        campoNombre.focus();
    }

    function cerrarModalForm() {
        modalForm.hidden = true;
    }

    async function guardarMaterial(evento) {
        evento.preventDefault();
        formError.textContent = '';

        const id = campoId.value;

        // Al editar no se envía cant_Material (MaterialUpdateRequest no la tiene); al crear
        // sí, como stock inicial.
        const request = id
            ? { nombre: campoNombre.value.trim() }
            : { nombre: campoNombre.value.trim(), cant_Material: parseInt(campoCantidad.value, 10) };

        const url = id ? `/Material/Edit/${id}` : '/Material/Create';

        try {
            const respuesta = await apiFetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                formError.textContent = datos.mensaje || 'No se pudo guardar el material';
                return;
            }

            if (id) {
                materiales = materiales.map(m => m.cod_Material === datos.cod_Material ? datos : m);
            } else {
                materiales.push(datos);
            }

            cerrarModalForm();
            renderizar();
            mostrarToast(id ? 'Datos guardados correctamente' : 'Material creado correctamente');
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
            const respuesta = await apiFetch(`/Material/Delete/${idParaEliminar}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                bajaError.textContent = datos.mensaje || 'No se pudo eliminar el material';
                return;
            }

            materiales = materiales.filter(m => m.cod_Material !== idParaEliminar);
            cerrarModalBaja();
            renderizar();
            mostrarToast('Material eliminado');
        } catch (ex) {
            bajaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Reactivar ----------

    async function reactivar(id) {
        try {
            const respuesta = await apiFetch(`/Material/Reactivar/${id}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                mostrarToast(datos.mensaje || 'No se pudo reactivar el material', true);
                return;
            }

            materiales = materiales.map(m => m.cod_Material === datos.cod_Material ? datos : m);
            renderizar();
            mostrarToast('Material reactivado correctamente');
        } catch (ex) {
            mostrarToast('Error de conexión con el servidor', true);
        }
    }

    // ---------- Modal Ajustar stock ----------

    function actualizarPreviewStock() {
        const base = materiales.find(m => m.cod_Material === idParaAjustarStock)?.cant_Material ?? 0;
        const cantidad = parseInt(campoAjusteCantidad.value, 10);
        const delta = Number.isFinite(cantidad) ? cantidad : 0;
        const resultado = tipoAjuste === 'ingreso' ? base + delta : base - delta;
        stockPreview.textContent = resultado;

        // Un egreso mayor al stock se marca en rojo y no se puede guardar (la API igual lo rechaza).
        const invalido = resultado < 0;
        stockPreview.parentElement.classList.toggle('modal-stock-preview--invalido', invalido);
        btnConfirmarStock.disabled = invalido;
        if (invalido) stockError.textContent = `El egreso supera el stock disponible (${base})`;
    }

    function elegirTipoAjuste(tipo) {
        tipoAjuste = tipo;
        btnIngreso.classList.toggle('btn-stock-tipo--activo', tipo === 'ingreso');
        btnEgreso.classList.toggle('btn-stock-tipo--activo', tipo === 'egreso');
        actualizarPreviewStock();
    }

    function abrirModalStock(id) {
        const material = materiales.find(m => m.cod_Material === id);
        if (!material) return;

        idParaAjustarStock = id;
        stockActual.textContent = material.cant_Material;
        campoAjusteCantidad.value = '';
        stockError.textContent = '';
        elegirTipoAjuste('ingreso');
        modalStock.hidden = false;
        campoAjusteCantidad.focus();
    }

    function cerrarModalStock() {
        modalStock.hidden = true;
        idParaAjustarStock = null;
    }

    async function confirmarAjusteStock() {
        if (idParaAjustarStock == null) return;
        stockError.textContent = '';

        const cantidad = parseInt(campoAjusteCantidad.value, 10);
        if (!Number.isFinite(cantidad) || cantidad <= 0) {
            stockError.textContent = 'Ingresá una cantidad mayor a 0';
            return;
        }

        const ajuste = tipoAjuste === 'ingreso' ? cantidad : -cantidad;

        try {
            const respuesta = await apiFetch(`/Material/AjustarStock/${idParaAjustarStock}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ ajuste })
            });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                stockError.textContent = datos.mensaje || 'No se pudo ajustar el stock';
                return;
            }

            materiales = materiales.map(m => m.cod_Material === datos.cod_Material ? datos : m);
            cerrarModalStock();
            renderizar();
            mostrarToast('Stock ajustado correctamente');
        } catch (ex) {
            stockError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Eventos ----------

    document.getElementById('btnAbrirAlta').addEventListener('click', abrirModalAlta);
    document.getElementById('btnCancelarForm').addEventListener('click', cerrarModalForm);
    formMaterial.addEventListener('submit', guardarMaterial);

    // El error del formulario no debe quedar visible después de que el usuario cambia algo.
    formMaterial.addEventListener('input', () => { formError.textContent = ''; });

    document.getElementById('btnCancelarBaja').addEventListener('click', cerrarModalBaja);
    document.getElementById('btnConfirmarBaja').addEventListener('click', confirmarBaja);

    btnIngreso.addEventListener('click', () => elegirTipoAjuste('ingreso'));
    btnEgreso.addEventListener('click', () => elegirTipoAjuste('egreso'));
    campoAjusteCantidad.addEventListener('input', () => { stockError.textContent = ''; actualizarPreviewStock(); });
    document.getElementById('btnCancelarStock').addEventListener('click', cerrarModalStock);
    document.getElementById('btnConfirmarStock').addEventListener('click', confirmarAjusteStock);

    listaEl.addEventListener('click', (evento) => {
        const boton = evento.target.closest('button[data-accion]');
        if (!boton) return;

        const id = parseInt(boton.dataset.id, 10);
        if (boton.dataset.accion === 'editar') abrirModalEdicion(id);
        if (boton.dataset.accion === 'eliminar') abrirModalBaja(id);
        if (boton.dataset.accion === 'reactivar') reactivar(id);
        if (boton.dataset.accion === 'ajustar-stock') abrirModalStock(id);
    });

    inputBuscar.addEventListener('input', renderizar);
    selectFiltroEstado.addEventListener('change', renderizar);

    [modalForm, modalBaja, modalStock].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    renderizar();
})();
