document.addEventListener('DOMContentLoaded', function () {

    const dropZone = document.getElementById('dropZone');
    const overlay = document.getElementById('dragOverlay');
    const browserHint = document.getElementById('browserHint');

    const fileInput = document.getElementById('fileInput');
    const carouselTrack = document.getElementById('carouselTrack');
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

    const textareas = document.querySelectorAll('textarea');
    textareas.forEach((element) => {
        element.style.resize = 'none';
        autosize(element);
    });

    setInterval(function () {

        textareas.forEach((element) => {
            if (element.classList.contains("input-validation-error")) {
                element.classList.add("error");
            } else {
                element.classList.remove("error");
            }
            const label = element.parentElement.querySelector('.floating-label');
            if (element.classList.contains("input-validation-error")) {
                label.color = "#ff4500";
            } else {
                label.color = "#8b949e";
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

    let images = [];
    let deletedImageIds = [];
    let currentIndex = 0;

    if (typeof existingImages !== "undefined" && existingImages.length > 0) {
        existingImages.forEach(img => {
            images.push({
                id: img.id,
                url: img.imgUrl,
                publicId: img.publicId,
                isExisting: true
            });
        });

        images = images.filter(img => (img.isExisting && img.url !== undefined));
    }

    const isFirefox = navigator.userAgent.toLowerCase().includes('firefox');
    if (isFirefox) browserHint.classList.remove('d-none');

    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(event => {
        dropZone.addEventListener(event, e => {
            e.preventDefault();
            e.stopPropagation();
        });
    });

    document.addEventListener('keydown', function (e) {
        if (images.length === 0) return;

        switch (e.key) {
            case 'ArrowRight':
                nextImg();
                break;
            case 'ArrowLeft':
                prevImg();
                break;
        }
    });

    dropZone.addEventListener('dragover', () => {
        overlay.classList.remove('d-none');
        dropZone.classList.add('drag-over');
    });

    dropZone.addEventListener('dragleave', () => {
        overlay.classList.add('d-none');
        dropZone.classList.remove('drag-over');
    });

    dropZone.addEventListener('drop', (e) => {
        overlay.classList.add('d-none');
        dropZone.classList.remove('drag-over');
        handleFiles(e.dataTransfer.files);
    });

    addBtnEmpty?.addEventListener('click', () => fileInput.click());
    addMorePhotosBtn?.addEventListener('click', () => fileInput.click());
    prevBtn?.addEventListener('click', prevImg);
    nextBtn?.addEventListener('click', nextImg);
    deleteCurrentBtn?.addEventListener('click', () => removeImageAtIndex(currentIndex));

    if (editModal) {
        editModal.addEventListener('show.bs.modal', renderModalGrid);
    }

    fileInput.addEventListener('change', function () {
        if (this.files.length > 0) handleFiles(this.files);
    });


    function handleFiles(files) {
        Array.from(files).forEach((file, index) => {
            if (!file.type.startsWith('image/')) return;

            images.push({
                file: file,
                isExisting: false
            });
        });

        currentIndex = images.length - 1;
        renderAll();
    }

    function renderAll() {
        syncFileInput();
        renderCarousel();
        renderDots();

        if (images.length === 0) {
            emptyState.classList.remove('d-none');
            previewContainer.classList.add('d-none');
        } else {
            emptyState.classList.add('d-none');
            previewContainer.classList.remove('d-none');
        }
    }

    function renderCarousel() {
        carouselTrack.innerHTML = '';

        images.forEach(img => {

            //if (typeof img.url !== 'undefined') {
            const el = document.createElement('img');
            el.className = 'carousel-img';

            el.src = img.isExisting
                ? img.url
                : URL.createObjectURL(img.file);

            el.addEventListener('click', () => {
                openImageModal(el.src);
            });

            carouselTrack.appendChild(el);
            // }
        });

        updateCarousel();
    }

    document.getElementById('modalPreviewImage').addEventListener('click', () => {
        bootstrap.Modal.getInstance(document.getElementById('imagePreviewModal')).hide();
    });

    function openImageModal(src) {
        const modalImg = document.getElementById('modalPreviewImage');
        modalImg.src = src;

        const modal = new bootstrap.Modal(document.getElementById('imagePreviewModal'));
        modal.show();
    }

    function updateCarousel() {
        carouselTrack.style.transform = `translateX(-${currentIndex * 100}%)`;
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

        renderAll();
    }

    function nextImg() {
        if (currentIndex < images.length - 1) {
            currentIndex++;
            updateCarousel();
            renderDots();
        }
    }

    function prevImg() {
        if (currentIndex > 0) {
            currentIndex--;
            updateCarousel();
            renderDots();
        }
    }

    function syncFileInput() {
        const dt = new DataTransfer();

        images.forEach(img => {
            if (!img.isExisting) {
                dt.items.add(img.file);
            }
        });

        fileInput.files = dt.files;
    }


    async function showNoImagesMessage() {
        const translations = {
            "en": {
                noImagesSelected: "No images selected"
            },
            "bg": {
                noImagesSelected: "Няма избрани изображения"
            },
            "de": {
                noImagesSelected: "Keine Bilder ausgewählt"
            },
            "es": {
                noImagesSelected: "No se han seleccionado imágenes"
            },
            "fr": {
                noImagesSelected: "Aucune image sélectionnée"
            }
        };
        const culture = await getCurrentCulture();
        const msg = translations[culture].noImagesSelected;

        modalGrid.innerHTML = `<p class="text-center text-muted w-100">${msg}</p>`;
    }

    function renderModalGrid() {
        modalGrid.innerHTML = '';

        if (images.length === 0) {
            showNoImagesMessage();
            return;
        }

        images.forEach((img, index) => {
            const card = document.createElement('div');
            card.className = 'thumb-card';

            const render = (src) => {
                card.innerHTML = `
                    <img src="${src}" />
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
                render(URL.createObjectURL(img.file));
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

    renderAll();
});