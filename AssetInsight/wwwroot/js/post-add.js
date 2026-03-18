document.addEventListener('DOMContentLoaded', function () {

    const dropZone = document.getElementById('dropZone');
    const overlay = document.getElementById('dragOverlay');
    const browserHint = document.getElementById('browserHint');

    // Detect Firefox
    const isFirefox = navigator.userAgent.toLowerCase().includes('firefox');
    if (isFirefox) {
        browserHint.classList.remove('d-none');
    }

    // Show overlay on drag
    dropZone.addEventListener('dragenter', () => {
        overlay.classList.remove('d-none');
    });

    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        overlay.classList.remove('d-none');
    });

    // Hide overlay when leaving
    dropZone.addEventListener('dragleave', () => {
        overlay.classList.add('d-none');
    });

    // Hide on drop
    dropZone.addEventListener('drop', () => {
        overlay.classList.add('d-none');
    });

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

    // --- Selectors ---
    const fileInput = document.getElementById('fileInput');
    //const dropZone = document.getElementById('dropZone');
    const mainPreview = document.getElementById('mainPreview');
    const dotIndicators = document.getElementById('dotIndicators');
    const emptyState = document.getElementById('emptyState');
    const previewContainer = document.getElementById('previewContainer');
    const modalGrid = document.getElementById('modalGrid');

    // Buttons
    const addBtnEmpty = document.getElementById('addBtnEmpty');
    const deleteCurrentBtn = document.getElementById('deleteCurrentBtn');
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    let uploadedFiles = [];
    let currentIndex = 0;

    // --- Initialization ---

    // Refresh modal grid whenever it's opened
    const editModal = document.getElementById('editGalleryModal');
    if (editModal) {
        editModal.addEventListener('show.bs.modal', renderModalGrid);
    }

    const addMorePhotosBtn = document.getElementById('addMorePhotosBtn');
    if (addMorePhotosBtn) {
        addMorePhotosBtn.addEventListener('click', () => fileInput.click());
    }

    // --- Event Listeners ---
    if (addBtnEmpty) addBtnEmpty.addEventListener('click', () => fileInput.click());
    if (prevBtn) prevBtn.addEventListener('click', prevImg);
    if (nextBtn) nextBtn.addEventListener('click', nextImg);
    if (deleteCurrentBtn) deleteCurrentBtn.addEventListener('click', () => removeImageAtIndex(currentIndex));

    fileInput.addEventListener('change', function () {
        if (this.files.length > 0) handleFiles(this.files);
        //this.value = ''; // Reset to allow re-uploading same file
    });

    // --- Drag and Drop ---
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

    // --- Logic Functions ---

    function handleFiles(files) {
        const fileArray = Array.from(files);//.filter(f => f.type.startsWith('image/'));
        const newFiles = [];

        fileArray.forEach((file) => {
            console.log('File: ', file.type)
            if (file.type.startsWith('image/')) {
                newFiles.push(file);
            }
        });

        if (newFiles.length) {
            uploadedFiles = uploadedFiles.concat(newFiles);
            // Jump to the first of the newly added images
            currentIndex = uploadedFiles.length - newFiles.length;
            syncUI();

            const editModal = document.getElementById('editGalleryModal');
            if (editModal && editModal.classList.contains('show')) {
                renderModalGrid();
            }
       }
    }

    /**
     * The Single Source of Truth for the UI
     * Updates the hidden input, main preview, and navigation dots.
     */
    function syncUI() {
        // 1. Sync hidden file input for the C# Controller
        const dt = new DataTransfer();
        uploadedFiles.forEach(file => dt.items.add(file));
        fileInput.files = dt.files;

        // 2. Toggle Empty State vs Preview
        if (uploadedFiles.length === 0) {
            emptyState.classList.remove('d-none');
            previewContainer.classList.add('d-none');
            mainPreview.src = '';
            return;
        }

        emptyState.classList.add('d-none');
        previewContainer.classList.remove('d-none');

        // 3. Update Main Image
        const reader = new FileReader();
        reader.onload = (e) => {
            mainPreview.src = e.target.result;
        };
        reader.readAsDataURL(uploadedFiles[currentIndex]);

        // 4. Update Bottom Dots (Capsule style)
        renderDots();
    }

    function renderDots() {
        dotIndicators.innerHTML = '';
        uploadedFiles.forEach((_, index) => {
            const dot = document.createElement('div');
            dot.className = `dot ${index === currentIndex ? 'active' : ''}`;
            dotIndicators.appendChild(dot);
        });
    }

    async function getCurrentCulture() {
        const response = await fetch('/Language/GetCurrentCulture');
        const data = await response.json();
        return data.culture;
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

        if (uploadedFiles.length === 0) {
            showNoImagesMessage();
            return;
        }

        uploadedFiles.forEach((file, index) => {
            const reader = new FileReader();
            reader.onload = (e) => {
                const card = document.createElement('div');
                card.className = 'thumb-card';
                card.innerHTML = `
                    <img src="${e.target.result}" style="max-height: 150px; object-fit: contain;" />
                    <div class="thumb-overlay">
                        <button type="button" class="btn-reddit-circle delete-thumb" data-index="${index}">
                            <i class="bi bi-trash3"></i>
                        </button>
                    </div>
                `;

                // Individual delete button in the grid
                card.querySelector('.delete-thumb').addEventListener('click', function () {
                    const idx = parseInt(this.getAttribute('data-index'));
                    removeImageAtIndex(idx);
                    renderModalGrid(); // Refresh grid after delete
                });

                modalGrid.appendChild(card);
            };
            reader.readAsDataURL(file);
        });
    }

    function removeImageAtIndex(index) {
        uploadedFiles.splice(index, 1);

        // Ensure currentIndex isn't pointing to a non-existent index
        if (currentIndex >= uploadedFiles.length) {
            currentIndex = Math.max(0, uploadedFiles.length - 1);
        }

        syncUI();
    }

    // --- Navigation ---

    function nextImg() {
        if (currentIndex < uploadedFiles.length - 1) {
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
});

/*const form = document.querySelectorAll('form');
form.forEach((element) => {
    element.addEventListener('submit', function (e) {
        e.preventDefault();
        if (fileInput.files.length === 0) {
            console.log('No files selected');
        } else {
            // Loop through the files
            for (var i = 0; i < fileInput.files.length; i++) {
                console.log('File Name: ' + fileInput.files[i].name);
            }
        }

    })
});*/