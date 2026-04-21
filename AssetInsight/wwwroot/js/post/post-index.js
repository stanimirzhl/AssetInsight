// Global Variables

let currentPage = 1;
let isLoading = false;
let hasMorePosts = true;

const postList = document.getElementById('post-list');
const sentinel = document.getElementById('loading-sentinel');
const skeletonTemplate = document.getElementById('skeleton-template');

const observer = new IntersectionObserver(async (entries) => {
    if (entries[0].isIntersecting && !isLoading && hasMorePosts) {
        await loadPosts();
    }
}, {
    threshold: 0.1,
    //rootMargin: '100px'
});


if (sentinel) observer.observe(sentinel);

// Functions

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
    const postList = document.getElementById('post-list');

    if (postList) {
        postList.addEventListener('click', async function (event) {
            const voteBtn = event.target.closest('.js-vote-btn');
            if (voteBtn) {
                event.preventDefault();
                const container = voteBtn.closest('.reddit-voting');
                const postId = container.dataset.postId;
                const isUpVote = voteBtn.dataset.isUpvote === 'true';

                await handleVote(postId, isUpVote, voteBtn);
                return; 
            }

            const saveBtn = event.target.closest('.js-save-btn');
            if (saveBtn) {
                event.preventDefault();
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
                const postId = saveBtn.dataset.postId;

                try {
                    const response = await fetch(`/Post/ToggleSave?postId=${postId}`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': token,
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    });

                    const contentType = response.headers.get("content-type");
                    let data;
                    if (contentType && contentType.includes("application/json")) {
                        data = await response.json();
                    }

                    if (data?.loginUrl) {
                        window.location.href = data.loginUrl;
                        return;
                    }

                    if (response.ok && data) {
                        showToast(data.message);
                    }
                } catch (error) {
                    console.error('Error:', error);
                }
            }
        });
    }

    async function handleVote(postId, isUpVote, btnElement) {
        const container = btnElement.closest('.reddit-voting');
        const scoreLabel = container.querySelector('.vote-count');
        const upBtn = container.querySelector('.vote-btn.up');
        const downBtn = container.querySelector('.vote-btn.down');

        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        const url = `/PostReaction/React?postId=${postId}&isUpVote=${isUpVote}`;

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            const contentType = response.headers.get("content-type");
            let data = null;

            if (contentType && contentType.includes("application/json")) {
                data = await response.json();
            }

            if (data && data.loginUrl) {
                window.location.href = data.loginUrl;
                return;
            }

            if (response.ok && data) {
                scoreLabel.innerText = data.score;

                upBtn.classList.remove('active');
                downBtn.classList.remove('active');

                if (data.status === 'upvoted') upBtn.classList.add('active');
                if (data.status === 'downvoted') downBtn.classList.add('active');
            } else {
                console.warn("Vote action returned non-OK response or no JSON data.");
            }
        } catch (err) {
            console.error("Voting failed due to network or parsing error:", err);
        }
    }
    /*
    document.querySelectorAll('.js-save-btn').forEach(btn => {
        btn.addEventListener('click', async function (event) {
            event.preventDefault();

            const postId = btn.dataset.postId;
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            try {
                const response = await fetch(`/Post/ToggleSave?postId=${postId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                const contentType = response.headers.get("content-type");

                let data;
                if (contentType && contentType.includes("application/json")) {
                    data = await response.json();
                }

                if (data?.loginUrl) {
                    window.location.href = data.loginUrl;
                    return;
                }

                if (response.ok && data) {
                    showToast(data.message);
                }
            } catch (error) {
                console.error('Error:', error);
            }
        });
    });*/
});

