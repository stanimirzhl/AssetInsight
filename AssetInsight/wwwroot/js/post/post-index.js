let currentPage = 1;
let isLoading = false;
let hasMorePosts = true;

const postList = document.getElementById('post-list');
const sentinel = document.getElementById('loading-sentinel');
const skeletonTemplate = document.getElementById('skeleton-template');

const observer = new IntersectionObserver((entries) => {
    if (entries[0].isIntersecting && !isLoading && hasMorePosts) {
        loadPosts();
    }
}, {
    threshold: 0.1,
    //rootMargin: '100px'
});

if (sentinel) observer.observe(sentinel);

async function loadPosts() {
    if (isLoading || !hasMorePosts) return;
    isLoading = true;

    const skeletons = [];
    const clone = skeletonTemplate.content.cloneNode(true);
    const div = document.createElement('div');
    div.appendChild(clone);
    postList.appendChild(div);
    skeletons.push(div);

    try {
        const nextPage = currentPage + 1;
        const urlParams = new URLSearchParams(window.location.search);
        const tag = urlParams.get('tag') || '';

        const response = await fetch(`?page=${nextPage}&tag=${tag}`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        const html = await response.text();
        skeletons.forEach(s => s.remove());

        if (!response.ok || html.trim() === "") {

            hasMorePosts = false;

            let culture = "en";
            try {
                culture = await getCurrentCulture();
            } catch (e) { culture = "en"; }

            const translations = {
                "en": "No more posts to show",
                "bg": "Няма повече публикации",
                "de": "Keine weiteren Beiträge",
                "es": "No hay más publicaciones",
                "fr": "Plus de publications disponibles"
            };

            const msg = (translations[culture] || translations["en"]).noMorePosts || translations[culture] || translations["en"];
            sentinel.innerHTML = `<p class='text-center mt-4 text-muted'>${msg}.</p>`;
            observer.unobserve(sentinel);
        } else {
            postList.insertAdjacentHTML('beforeend', html);
            if (typeof updateTimeAgo === 'function') updateTimeAgo();
            currentPage = nextPage;
        }
    } catch (err) {
        console.error("Fetch error:", err);
        skeletons.forEach(s => s.remove());
    } finally {
        isLoading = false;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const deleteModalElement = document.getElementById('deletePostModal');
    const deleteForm = document.getElementById('deletePostForm');
    const modalTitle = document.getElementById('modalPostTitle');

    let currentPostId = null;

    if (deleteModalElement) {
        deleteModalElement.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            currentPostId = button.getAttribute('data-post-id');
            const postTitle = button.getAttribute('data-post-title');
            if (modalTitle) modalTitle.textContent = postTitle || "";
        });

        deleteModalElement.addEventListener('hidden.bs.modal', () => {
            currentPostId = null;
        });
    }

    if (deleteForm) {
        deleteForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (deleteForm.querySelector(".btn-confirm-delete").classList.contains("Inactive")) {
                return;
            }
            deleteForm.querySelector(".btn-confirm-delete").classList.add("Inactive");
            if (!currentPostId) return;

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            try {
                const response = await fetch(`/Post/Delete/${currentPostId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (response.ok) {
                    const postElement = document.getElementById(`post-${currentPostId}`);
                    if (postElement) {
                        postElement.style.transition = "opacity 0.3s ease";
                        postElement.style.opacity = "0";
                        setTimeout(() => postElement.remove(), 300);
                    }

                    const modalInstance = bootstrap.Modal.getInstance(deleteModalElement);
                    deleteForm.querySelector(".btn-confirm-delete").classList.remove("Inactive");
                    modalInstance.hide();
                } else {
                    deleteForm.querySelector(".btn-confirm-delete").classList.remove("Inactive");
                    alert("Error deleting post.");
                }
            } catch (err) {

                deleteForm.querySelector(".btn-confirm-delete").classList.remove("Inactive");
                console.error("Network error:", err);
            }
        });
    }
});