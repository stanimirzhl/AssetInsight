document.addEventListener('DOMContentLoaded', function () {

    const dropZone = document.getElementById('dropZone');
    const overlay = document.getElementById('dragOverlay');
    const browserHint = document.getElementById('browserHint');

    const isFirefox = navigator.userAgent.toLowerCase().includes('firefox');
    if (isFirefox) {
        browserHint.classList.remove('d-none');
    }

    dropZone.addEventListener('dragenter', () => {
        overlay.classList.remove('d-none');
    });

    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        overlay.classList.remove('d-none');
    });

    dropZone.addEventListener('dragleave', () => {
        overlay.classList.add('d-none');
    });

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

    const fileInput = document.getElementById('fileInput');
    //const dropZone = document.getElementById('dropZone');
    const mainPreview = document.getElementById('mainPreview');
    const dotIndicators = document.getElementById('dotIndicators');
    const emptyState = document.getElementById('emptyState');
    const previewContainer = document.getElementById('previewContainer');
    const modalGrid = document.getElementById('modalGrid');

    const addBtnEmpty = document.getElementById('addBtnEmpty');
    const deleteCurrentBtn = document.getElementById('deleteCurrentBtn');
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    let uploadedFiles = [];
    let currentIndex = 0;

    const editModal = document.getElementById('editGalleryModal');
    if (editModal) {
        editModal.addEventListener('show.bs.modal', renderModalGrid);
    }

    const addMorePhotosBtn = document.getElementById('addMorePhotosBtn');
    if (addMorePhotosBtn) {
        addMorePhotosBtn.addEventListener('click', () => fileInput.click());
    }

    if (addBtnEmpty) addBtnEmpty.addEventListener('click', () => fileInput.click());
    if (prevBtn) prevBtn.addEventListener('click', prevImg);
    if (nextBtn) nextBtn.addEventListener('click', nextImg);
    if (deleteCurrentBtn) deleteCurrentBtn.addEventListener('click', () => removeImageAtIndex(currentIndex));

    fileInput.addEventListener('change', function () {
        if (this.files.length > 0) handleFiles(this.files);
        //this.value = '';
    });

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

    function handleFiles(files) {
        const fileArray = Array.from(files);//.filter(f => f.type.startsWith('image/'));
        const newFiles = [];

        fileArray.forEach((file, index) => {
            //console.log('File: ', file.type)
            if (file.type.startsWith('image/')) {

                if (file.name.startsWith('download')) {
                    const extension = file.type.split('/')[1] || 'png';
                    fileName = `image_${Date.now()}_${index}.${extension}`;
                    file = new File([file], fileName, { type: file.type });
                }

                newFiles.push(file);
            }
        });

        if (newFiles.length) {
            uploadedFiles = uploadedFiles.concat(newFiles);

            currentIndex = uploadedFiles.length - newFiles.length;
            syncUI();

            const editModal = document.getElementById('editGalleryModal');
            if (editModal && editModal.classList.contains('show')) {
                renderModalGrid();
            }
       }
    }

    function syncUI() {
        const dt = new DataTransfer();
        uploadedFiles.forEach(file => dt.items.add(file));
        fileInput.files = dt.files;

        if (uploadedFiles.length === 0) {
            emptyState.classList.remove('d-none');
            previewContainer.classList.add('d-none');
            mainPreview.src = '';
            return;
        }

        emptyState.classList.add('d-none');
        previewContainer.classList.remove('d-none');

        const reader = new FileReader();
        reader.onload = (e) => {
            mainPreview.src = e.target.result;
        };
        reader.readAsDataURL(uploadedFiles[currentIndex]);

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

                card.querySelector('.delete-thumb').addEventListener('click', function () {
                    const idx = parseInt(this.getAttribute('data-index'));
                    removeImageAtIndex(idx);
                    renderModalGrid(); 
                });

                modalGrid.appendChild(card);
            };
            reader.readAsDataURL(file);
        });
    }

    function removeImageAtIndex(index) {
        uploadedFiles.splice(index, 1);

        if (currentIndex >= uploadedFiles.length) {
            currentIndex = Math.max(0, uploadedFiles.length - 1);
        }

        syncUI();
    }

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