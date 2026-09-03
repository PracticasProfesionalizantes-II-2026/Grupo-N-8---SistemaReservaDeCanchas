(() => {
    let materiales = Array.isArray(materialesIniciales) ? materialesIniciales : [];
    let idParaEliminar = null;

    const listaEl = document.getElementById('listaMateriales');
    const inputBuscar = document.getElementById('inputBuscar');

    const modalForm = document.getElementById('modalForm');
    const modalFormTitulo = document.getElementById('modalFormTitulo');
    const formMaterial = document.getElementById('formMaterial');
    const formError = document.getElementById('formError');
    const campoId = document.getElementById('materialId');
    const campoNombre = document.getElementById('campoNombre');
    const campoCantidad = document.getElementById('campoCantidad');

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

    function escapeHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto;
        return div.innerHTML;
    }

    // ---------- Render ----------

    function renderizar() {
        const texto = inputBuscar.value.trim().toLowerCase();

        const filtrados = materiales.filter(m => {
            return !texto || m.nombre.toLowerCase().includes(texto);
        });

        if (filtrados.length === 0) {
            listaEl.innerHTML = '<div class="productos-vacio">No se encontraron materiales.</div>';
            return;
        }

        listaEl.innerHTML = filtrados.map(m => {
            const stockBajo = m.cant_Material <= 5;
            return `
                <div class="producto-card" data-id="${m.cod_Material}">
                    <div class="producto-card__tipo producto-card__tipo--material">🏐</div>
                    <div class="producto-card__info">
                        <div class="producto-card__nombre">${escapeHtml(m.nombre)}</div>
                        <div class="producto-card__meta">
                            <span class="producto-card__stock ${stockBajo ? 'producto-card__stock--bajo' : ''}">Stock: ${m.cant_Material}</span>
                        </div>
                    </div>
                    <div class="producto-card__acciones">
                        <button type="button" class="btn-editar" data-accion="editar" data-id="${m.cod_Material}">✏️ Editar</button>
                        <button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${m.cod_Material}" title="Eliminar">✕</button>
                    </div>
                </div>`;
        }).join('');
    }

    // ---------- Modal Alta / Edición ----------

    function abrirModalAlta() {
        modalFormTitulo.textContent = 'Nuevo Material';
        campoId.value = '';
        formMaterial.reset();
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
        campoCantidad.value = material.cant_Material;
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

        const request = {
            nombre: campoNombre.value.trim(),
            cant_Material: parseInt(campoCantidad.value, 10)
        };

        const id = campoId.value;
        const url = id ? `/Material/Edit/${id}` : '/Material/Create';

        try {
            const respuesta = await fetch(url, {
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
            const respuesta = await fetch(`/Material/Delete/${idParaEliminar}`, { method: 'POST' });
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

    // ---------- Eventos ----------

    document.getElementById('btnAbrirAlta').addEventListener('click', abrirModalAlta);
    document.getElementById('btnCancelarForm').addEventListener('click', cerrarModalForm);
    formMaterial.addEventListener('submit', guardarMaterial);

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

    [modalForm, modalBaja].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    renderizar();
})();
