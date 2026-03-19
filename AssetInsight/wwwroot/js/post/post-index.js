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
}, { threshold: 0.2 });

observer.observe(sentinel);

async function loadPosts() {
    isLoading = true;

    const skeletons = [];
    for (let i = 0; i < 1; i++) {
        const clone = skeletonTemplate.content.cloneNode(true);
        const div = document.createElement('div');
        div.appendChild(clone);
        postList.appendChild(div);
        skeletons.push(div);
    }

    try {
        const nextPage = currentPage + 1;
        const urlParams = new URLSearchParams(window.location.search);
        const tag = urlParams.get('tag');
        const response = await fetch(`?page=${nextPage}&tag=${tag ?? ''}`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        const html = await response.text();

        skeletons.forEach(s => s.remove());

        if (!response.ok || html.trim() === "") {
            hasMorePosts = false;
            const culture = await getCurrentCulture();
            const translations = {
                "en": { noMorePosts: "No more posts to show" },
                "bg": { noMorePosts: "Няма повече публикации" },
                "de": { noMorePosts: "Keine weiteren Beiträge" },
                "es": { noMorePosts: "No hay más publicaciones" },
                "fr": { noMorePosts: "Plus de publications disponibles" }
            };
            sentinel.innerHTML = `<p class='text-center mt-4 text-muted'>${translations[culture].noMorePosts}.</p>`;
            observer.unobserve(sentinel);
        } else {
            postList.insertAdjacentHTML('beforeend', html);
            updateTimeAgo();
            currentPage = nextPage;
        }
    } catch (err) {
        console.error("Fetch error:", err);
        skeletons.forEach(s => s.remove());
    } finally {
        isLoading = false;
    }
}

async function savePost(btn, postId) {
    try {
        const response = await fetch(`/Post/Save/${postId}`, {
            method: 'POST',
            headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value }
        });

        if (response.ok) {
            btn.classList.add('active');
            btn.querySelector('.save-label').innerText = "Saved";
        }
    } catch (err) {
        console.error("Save failed", err);
    }
}