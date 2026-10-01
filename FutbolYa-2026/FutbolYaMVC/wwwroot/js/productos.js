// Módulo de la página de productos: los datos iniciales llegan serializados desde la vista
// Razor (productosIniciales); este script renderiza el listado y los modales de alta/edición/
// ajuste de stock, y llama a los endpoints de ProductoController vía apiFetch.
(() => {
    let productos = Array.isArray(productosIniciales) ? productosIniciales : [];
    let idParaEliminar = null;

    const listaEl = document.getElementById('listaProductos');
    const inputBuscar = document.getElementById('inputBuscar');
    const selectFiltroTipo = document.getElementById('selectFiltroTipo');
    const selectFiltroEstado = document.getElementById('selectFiltroEstado');

    const modalForm = document.getElementById('modalForm');
    const modalFormTitulo = document.getElementById('modalFormTitulo');
    const formProducto = document.getElementById('formProducto');
    const formError = document.getElementById('formError');
    const campoId = document.getElementById('productoId');
    const campoNombre = document.getElementById('campoNombre');
    const campoTipo = document.getElementById('campoTipo');
    const grupoCantidad = document.getElementById('grupoCantidad');
    const campoCantidad = document.getElementById('campoCantidad');
    const campoPrecio = document.getElementById('campoPrecio');

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

    function formatearPrecio(valor) {
        return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', minimumFractionDigits: 0 })
            .format(valor)
            .replace('ARS', '$')
            .replace(/\s/g, '');
    }

    function iconoTipo(tipo) {
        return tipo === 'Bebida' ? '🥤' : '🍔';
    }

    // ---------- Render ----------

    function renderizar() {
        const texto = inputBuscar.value.trim().toLowerCase();
        const tipo = selectFiltroTipo.value;
        const estado = selectFiltroEstado.value; // '', 'activo', 'baja'

        const filtrados = productos.filter(p => {
            const coincideTexto = !texto || p.nombre.toLowerCase().includes(texto);
            const coincideTipo = !tipo || p.tipo === tipo;
            const coincideEstado = !estado || (estado === 'activo' ? p.activo : !p.activo);
            return coincideTexto && coincideTipo && coincideEstado;
        });

        if (filtrados.length === 0) {
            listaEl.innerHTML = '<div class="productos-vacio">No se encontraron productos.</div>';
            return;
        }

        listaEl.innerHTML = filtrados.map(p => {
            const stockBajo = p.cantidad <= 5;
            const claseTipo = p.tipo === 'Bebida' ? 'producto-card__tipo--bebida' : 'producto-card__tipo--comida';
            const acciones = p.activo
                ? `<button type="button" class="btn-editar" data-accion="editar" data-id="${p.cod_Producto}">✏️ Editar</button>
                   <button type="button" class="btn-ajustar-stock" data-accion="ajustar-stock" data-id="${p.cod_Producto}">📦 Ajustar stock</button>
                   <button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${p.cod_Producto}" title="Eliminar">✕</button>`
                : `<button type="button" class="btn-reactivar" data-accion="reactivar" data-id="${p.cod_Producto}">↺ Reactivar</button>`;

            return `
                <div class="producto-card ${p.activo ? '' : 'producto-card--baja'}" data-id="${p.cod_Producto}">
                    <div class="producto-card__tipo ${claseTipo}">${iconoTipo(p.tipo)}</div>
                    <div class="producto-card__info">
                        <div class="producto-card__nombre">${escapeHtml(p.nombre)} ${p.activo ? '' : '<span class="badge-baja">Baja</span>'}</div>
                        <div class="producto-card__meta">
                            <span class="producto-card__stock ${stockBajo ? 'producto-card__stock--bajo' : ''}">Stock: ${p.cantidad}</span>
                            <span class="producto-card__precio">${formatearPrecio(p.precio)}</span>
                        </div>
                    </div>
                    <div class="producto-card__acciones">
                        ${acciones}
                    </div>
                </div>`;
        }).join('');
    }

    function escapeHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto;
        return div.innerHTML;
    }

    // ---------- Modal Alta / Edición ----------

    // El mismo modal se reusa para alta/edición; el campo Stock solo se muestra
    // (y es obligatorio) al crear — al editar el stock se ajusta con el modal aparte.
    function abrirModalAlta() {
        modalFormTitulo.textContent = 'Nuevo Producto';
        campoId.value = '';
        formProducto.reset();
        campoTipo.value = 'Bebida';
        grupoCantidad.hidden = false;
        campoCantidad.required = true;
        formError.textContent = '';
        modalForm.hidden = false;
        campoNombre.focus();
    }

    function abrirModalEdicion(id) {
        const producto = productos.find(p => p.cod_Producto === id);
        if (!producto) return;

        modalFormTitulo.textContent = 'Editar Producto';
        campoId.value = producto.cod_Producto;
        campoNombre.value = producto.nombre;
        campoTipo.value = producto.tipo;
        campoPrecio.value = producto.precio;
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

    async function guardarProducto(evento) {
        evento.preventDefault();
        formError.textContent = '';

        const id = campoId.value;

        // Al editar no se envía cantidad (ProductoUpdateRequest no la tiene); al crear sí,
        // como stock inicial.
        const request = id
            ? {
                nombre: campoNombre.value.trim(),
                precio: parseFloat(campoPrecio.value),
                tipo: campoTipo.value
            }
            : {
                nombre: campoNombre.value.trim(),
                cantidad: parseInt(campoCantidad.value, 10),
                precio: parseFloat(campoPrecio.value),
                tipo: campoTipo.value
            };

        const url = id ? `/Producto/Edit/${id}` : '/Producto/Create';

        try {
            const respuesta = await apiFetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                formError.textContent = datos.mensaje || 'No se pudo guardar el producto';
                return;
            }

            if (id) {
                productos = productos.map(p => p.cod_Producto === datos.cod_Producto ? datos : p);
            } else {
                productos.push(datos);
            }

            cerrarModalForm();
            renderizar();
            mostrarToast(id ? 'Datos guardados correctamente' : 'Producto creado correctamente');
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
            const respuesta = await apiFetch(`/Producto/Delete/${idParaEliminar}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                bajaError.textContent = datos.mensaje || 'No se pudo eliminar el producto';
                return;
            }

            productos = productos.filter(p => p.cod_Producto !== idParaEliminar);
            cerrarModalBaja();
            renderizar();
            mostrarToast('Producto eliminado');
        } catch (ex) {
            bajaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Reactivar ----------

    async function reactivar(id) {
        try {
            const respuesta = await apiFetch(`/Producto/Reactivar/${id}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                mostrarToast(datos.mensaje || 'No se pudo reactivar el producto', true);
                return;
            }

            productos = productos.map(p => p.cod_Producto === datos.cod_Producto ? datos : p);
            renderizar();
            mostrarToast('Producto reactivado correctamente');
        } catch (ex) {
            mostrarToast('Error de conexión con el servidor', true);
        }
    }

    // ---------- Modal Ajustar stock ----------

    function actualizarPreviewStock() {
        const base = productos.find(p => p.cod_Producto === idParaAjustarStock)?.cantidad ?? 0;
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
        const producto = productos.find(p => p.cod_Producto === id);
        if (!producto) return;

        idParaAjustarStock = id;
        stockActual.textContent = producto.cantidad;
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
            const respuesta = await apiFetch(`/Producto/AjustarStock/${idParaAjustarStock}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ ajuste })
            });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                stockError.textContent = datos.mensaje || 'No se pudo ajustar el stock';
                return;
            }

            productos = productos.map(p => p.cod_Producto === datos.cod_Producto ? datos : p);
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
    formProducto.addEventListener('submit', guardarProducto);

    // El error del formulario no debe quedar visible después de que el usuario cambia algo.
    formProducto.addEventListener('input', () => { formError.textContent = ''; });
    formProducto.addEventListener('change', () => { formError.textContent = ''; });

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
    selectFiltroTipo.addEventListener('change', renderizar);
    selectFiltroEstado.addEventListener('change', renderizar);

    [modalForm, modalBaja, modalStock].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    renderizar();
})();
