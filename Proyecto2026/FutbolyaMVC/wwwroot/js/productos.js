(() => {
    let productos = Array.isArray(productosIniciales) ? productosIniciales : [];
    let idParaEliminar = null;

    const listaEl = document.getElementById('listaProductos');
    const inputBuscar = document.getElementById('inputBuscar');
    const selectFiltroTipo = document.getElementById('selectFiltroTipo');

    const modalForm = document.getElementById('modalForm');
    const modalFormTitulo = document.getElementById('modalFormTitulo');
    const formProducto = document.getElementById('formProducto');
    const formError = document.getElementById('formError');
    const campoId = document.getElementById('productoId');
    const campoNombre = document.getElementById('campoNombre');
    const campoTipo = document.getElementById('campoTipo');
    const campoCantidad = document.getElementById('campoCantidad');
    const campoPrecio = document.getElementById('campoPrecio');

    const modalBaja = document.getElementById('modalBaja');
    const bajaError = document.getElementById('bajaError');

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

        const filtrados = productos.filter(p => {
            const coincideTexto = !texto || p.nombre.toLowerCase().includes(texto);
            const coincideTipo = !tipo || p.tipo === tipo;
            return coincideTexto && coincideTipo;
        });

        if (filtrados.length === 0) {
            listaEl.innerHTML = '<div class="productos-vacio">No se encontraron productos.</div>';
            return;
        }

        listaEl.innerHTML = filtrados.map(p => {
            const stockBajo = p.cantidad <= 5;
            const claseTipo = p.tipo === 'Bebida' ? 'producto-card__tipo--bebida' : 'producto-card__tipo--comida';
            return `
                <div class="producto-card" data-id="${p.cod_Producto}">
                    <div class="producto-card__tipo ${claseTipo}">${iconoTipo(p.tipo)}</div>
                    <div class="producto-card__info">
                        <div class="producto-card__nombre">${escapeHtml(p.nombre)}</div>
                        <div class="producto-card__meta">
                            <span class="producto-card__stock ${stockBajo ? 'producto-card__stock--bajo' : ''}">Stock: ${p.cantidad}</span>
                            <span class="producto-card__precio">${formatearPrecio(p.precio)}</span>
                        </div>
                    </div>
                    <div class="producto-card__acciones">
                        <button type="button" class="btn-editar" data-accion="editar" data-id="${p.cod_Producto}">✏️ Editar</button>
                        <button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${p.cod_Producto}" title="Eliminar">✕</button>
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

    function abrirModalAlta() {
        modalFormTitulo.textContent = 'Nuevo Producto';
        campoId.value = '';
        formProducto.reset();
        campoTipo.value = 'Bebida';
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
        campoCantidad.value = producto.cantidad;
        campoPrecio.value = producto.precio;
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

        const request = {
            nombre: campoNombre.value.trim(),
            cantidad: parseInt(campoCantidad.value, 10),
            precio: parseFloat(campoPrecio.value),
            tipo: campoTipo.value
        };

        const id = campoId.value;
        const url = id ? `/Producto/Edit/${id}` : '/Producto/Create';

        try {
            const respuesta = await fetch(url, {
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
            const respuesta = await fetch(`/Producto/Delete/${idParaEliminar}`, { method: 'POST' });
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

    // ---------- Eventos ----------

    document.getElementById('btnAbrirAlta').addEventListener('click', abrirModalAlta);
    document.getElementById('btnCancelarForm').addEventListener('click', cerrarModalForm);
    formProducto.addEventListener('submit', guardarProducto);

    document.getElementById('btnCancelarBaja').addEventListener('click', cerrarModalBaja);
    document.getElementById('btnConfirmarBaja').addEventListener('click', confirmarBaja);

    listaEl.addEventListener('click', (evento) => {
        const boton = evento.target.closest('button[data-accion]');
        if (!boton) return;

        const id = parseInt(boton.dataset.id, 10);
        if (boton.dataset.accion === 'editar') abrirModalEdicion(id);
        if (boton.dataset.accion === 'eliminar') abrirModalBaja(id);
    });

    inputBuscar.addEventListener('input', renderizar);
    selectFiltroTipo.addEventListener('change', renderizar);

    [modalForm, modalBaja].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    renderizar();
})();
