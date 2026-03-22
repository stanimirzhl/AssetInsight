document.addEventListener('DOMContentLoaded', function () {

    const dropZone = document.getElementById('dropZone');
    const overlay = document.getElementById('dragOverlay');
    const browserHint = document.getElementById('browserHint');

    const fileInput = document.getElementById('fileInput');
    const mainPreview = document.getElementById('mainPreview');
    const dotIndicators = document.getElementById('dotIndicators');
    const emptyState = document.getElementById('emptyState');
    const previewContainer = document.getElementById('previewContainer');
    const modalGrid = document.getElementById('modalGrid');

    const addBtnEmpty = document.getElementById('addBtnEmpty');
    const deleteCurrentBtn = document.getElementById('deleteCurrentBtn');
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    const addMorePhotosBtn = document.getElementById('addMorePhotosBtn');
    const editModal = document.getElementById('editGalleryModal');

    let images = []; 
    let deletedImageIds = [];
    let currentIndex = 0;
    console.log("existingImages raw:", existingImages);
    
    if (typeof existingImages !== "undefined" && existingImages.length > 0) {
        existingImages.forEach(img => {
            console.log(img);
            images.push({
                id: img.id,
                url: img.imgUrl,
                publicId: img.publicId,
                isExisting: true
            });
        });
    }

    const isFirefox = navigator.userAgent.toLowerCase().includes('firefox');
    if (isFirefox) browserHint.classList.remove('d-none');

    dropZone.addEventListener('dragenter', () => overlay.classList.remove('d-none'));
    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        overlay.classList.remove('d-none');
    });
    dropZone.addEventListener('dragleave', () => overlay.classList.add('d-none'));
    dropZone.addEventListener('drop', () => overlay.classList.add('d-none'));

    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(event => {
        dropZone.addEventListener(event, e => {
            e.preventDefault();
            e.stopPropagation();
        });
    });

    dropZone.addEventListener('dragover', () => dropZone.classList.add('drag-over'));
    dropZone.addEventListener('dragleave', () => dropZone.classList.remove('drag-over'));

    dropZone.addEventListener('drop', (e) => {
        dropZone.classList.remove('drag-over');
        handleFiles(e.dataTransfer.files);
    });

    const textareas = document.querySelectorAll('textarea');
    textareas.forEach((el) => {
        el.style.resize = 'none';
        autosize(el);
    });

    setInterval(() => {
        textareas.forEach((el) => {
            const label = el.parentElement.querySelector('.floating-label');

            if (el.classList.contains("input-validation-error")) {
                el.classList.add("error");
                label.style.color = "#ff4500";
            } else {
                el.classList.remove("error");
                label.style.color = "#8b949e";
            }
        });
    }, 100);

    textareas.forEach((textarea) => {
        const counter = document.querySelector(`span[data-for="${textarea.id}"]`);
        const max = textarea.dataset.max || textarea.maxLength;

        counter.textContent = `${textarea.value.length}/${max}`;

        textarea.addEventListener("input", () => {
            counter.textContent = `${textarea.value.length}/${max}`;
        });
    });

    if (addBtnEmpty) addBtnEmpty.addEventListener('click', () => fileInput.click());
    if (addMorePhotosBtn) addMorePhotosBtn.addEventListener('click', () => fileInput.click());
    if (prevBtn) prevBtn.addEventListener('click', prevImg);
    if (nextBtn) nextBtn.addEventListener('click', nextImg);
    if (deleteCurrentBtn) deleteCurrentBtn.addEventListener('click', () => removeImageAtIndex(currentIndex));

    if (editModal) {
        editModal.addEventListener('show.bs.modal', renderModalGrid);
    }

    fileInput.addEventListener('change', function () {
        if (this.files.length > 0) handleFiles(this.files);
    });

    function handleFiles(files) {
        const fileArray = Array.from(files);

        fileArray.forEach((file, index) => {
            if (!file.type.startsWith('image/')) return;

            if (file.name.startsWith('download')) {
                const extension = file.type.split('/')[1] || 'png';
                file = new File([file], `image_${Date.now()}_${index}.${extension}`, { type: file.type });
            }

            images.push({
                file: file,
                isExisting: false
            });
        });

        currentIndex = images.length - 1;
        syncUI();

        if (editModal && editModal.classList.contains('show')) {
            renderModalGrid();
        }
    }

    function syncUI() {

        const dt = new DataTransfer();
        images.forEach(img => {
            if (!img.isExisting) {
                dt.items.add(img.file);
            }
        });
        fileInput.files = dt.files;

        if (images.length === 0) {
            emptyState.classList.remove('d-none');
            previewContainer.classList.add('d-none');
            mainPreview.src = '';
            return;
        }

        emptyState.classList.add('d-none');
        previewContainer.classList.remove('d-none');

        const img = images[currentIndex];

        if (img.isExisting) {
            mainPreview.src = img.url;
        } else {
            const reader = new FileReader();
            reader.onload = (e) => mainPreview.src = e.target.result;
            reader.readAsDataURL(img.file);
        }

        renderDots();
    }

    function renderDots() {
        dotIndicators.innerHTML = '';

        images.forEach((_, index) => {
            const dot = document.createElement('div');
            dot.className = `dot ${index === currentIndex ? 'active' : ''}`;
            dotIndicators.appendChild(dot);
        });
    }

    function removeImageAtIndex(index) {
        const img = images[index];

        if (img.isExisting) {
            deletedImageIds.push({ id: img.id, publicId: img.publicId });
        }

        images.splice(index, 1);

        if (currentIndex >= images.length) {
            currentIndex = Math.max(0, images.length - 1);
        }

        syncUI();
    }

    function nextImg() {
        if (currentIndex < images.length - 1) {
            currentIndex++;
            syncUI();
        }
    }

    function prevImg() {
        if (currentIndex > 0) {
            currentIndex--;
            syncUI();
        }
    }

    function renderModalGrid() {
        modalGrid.innerHTML = '';

        if (images.length === 0) {
            modalGrid.innerHTML = `<p class="text-center text-muted w-100">No images selected</p>`;
            return;
        }

        images.forEach((img, index) => {

            const card = document.createElement('div');
            card.className = 'thumb-card';

            const render = (src) => {
                card.innerHTML = `
                    <img src="${src}" style="max-height: 150px; object-fit: contain;" />
                    <div class="thumb-overlay">
                        <button type="button" class="btn-reddit-circle delete-thumb" data-index="${index}">
                            <i class="bi bi-trash3"></i>
                        </button>
                    </div>
                `;

                card.querySelector('.delete-thumb').addEventListener('click', function () {
                    removeImageAtIndex(index);
                    renderModalGrid();
                });
            };

            if (img.isExisting) {
                render(img.url);
            } else {
                const reader = new FileReader();
                reader.onload = (e) => render(e.target.result);
                reader.readAsDataURL(img.file);
            }

            modalGrid.appendChild(card);
        });
    }

    const form = document.querySelector("form");
    if (form) {
        form.addEventListener("submit", () => {
            const input = document.getElementById("deletedImagesInput");
            if (input) {
                input.value = JSON.stringify(deletedImageIds);
            }
        });
    }

    syncUI();
});