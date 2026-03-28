let isFetching = false;

const observer = new IntersectionObserver(async (entries) => {
    const hasMoreInput = document.getElementById('hasMore');
    const hasMore = hasMoreInput && hasMoreInput.value === "true";

    if (entries[0].isIntersecting && hasMore && !isFetching) {
        await loadMoreComments();
    }
}, {
    threshold: 0.1,
    rootMargin: '100px'
});

const sentinel = document.getElementById('infinite-scroll-sentinel');
if (sentinel) observer.observe(sentinel);

async function loadMoreComments() {
    isFetching = true;

    const pageInput = document.getElementById('currentPage');
    const hasMoreInput = document.getElementById('hasMore');
    const loader = document.getElementById('loader');
    const statusMsg = document.getElementById('status-message');
    const postId = document.getElementById('postId').value;

    let nextPage = parseInt(pageInput.value) + 1;

    try {
        const response = await fetch(`/Comment/GetMoreComments?postId=${postId}&pageIndex=${nextPage}`);

        if (response.status === 204 || response.ok === false) {
            hasMoreInput.value = "false";
            loader.classList.add('d-none');
            statusMsg.classList.remove('d-none');
            statusMsg.innerText = "No more comments.";
            return;
        }

        const html = await response.text();

        if (!html || html.trim().length === 0) {
            hasMoreInput.value = "false";
            loader.classList.add('d-none');
            statusMsg.classList.remove('d-none');
            return;
        }

        document.getElementById('comments-container').insertAdjacentHTML('beforeend', html);
        pageInput.value = nextPage;

    } catch (err) {
        console.error("Infinite scroll error:", err);
        loader.classList.add('d-none');
    } finally {
        isFetching = false;
    }
}

document.addEventListener("click", async function (e) {
    if (e.target.classList.contains("load-replies-btn")) {
        const btn = e.target;
        const parentId = btn.dataset.commentId;
        const container = document.getElementById(`replies-container-${parentId}`);

        btn.disabled = true;
        btn.innerHTML = "Loading...";

        try {
            const response = await fetch(`/Comment/GetReplies?parentId=${parentId}`);
            const html = await response.text();
            container.innerHTML = html;
            btn.remove();
        } catch (err) {
            btn.disabled = false;
            btn.innerHTML = "Error loading. Try again?";
        }
    }
});

async function handleVote(postId, isUpVote, btnElement) {
    const container = btnElement.closest('.reddit-voting');
    const scoreLabel = container.querySelector('.vote-count');
    const upBtn = container.querySelector('.vote-btn.up');
    const downBtn = container.querySelector('.vote-btn.down');

    const url = `/PostReaction/React?postId=${postId}&isUpVote=${isUpVote}`;

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        });

        data = await response.json();

        if (data.loginUrl) {
            window.location.href = data.loginUrl;
            return;
        }

        if (response.ok) {
            //data = await response.json();

            scoreLabel.innerText = data.score;

            upBtn.classList.remove('active');
            downBtn.classList.remove('active');

            if (data.status === 'upvoted') upBtn.classList.add('active');
            if (data.status === 'downvoted') downBtn.classList.add('active');
        }
    } catch (err) {
        console.error("Voting failed:", err);
    }
}

async function handleCommentVote(commentId, isUpVote, btnElement) {
    const container = btnElement.closest('.comment-voting');
    const scoreLabel = container.querySelector('.vote-count-sm');
    const upBtn = container.querySelector('.vote-btn-sm.up');
    const downBtn = container.querySelector('.vote-btn-sm.down');
    const postId = document.getElementById('postId').value;

    if (btnElement.disabled) return;
    btnElement.disabled = true;

    try {
        const response = await fetch(`/CommentReaction/React?postId=${postId}&commentId=${commentId}&isUpVote=${isUpVote}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        });

        const data = await response.json();

        if (data.loginUrl) {
            window.location.href = data.loginUrl;
            return;
        }

        if (response.ok) {
            scoreLabel.innerText = data.score;

            upBtn.classList.remove('active');
            downBtn.classList.remove('active');

            if (data.status === 'upvoted') upBtn.classList.add('active');
            if (data.status === 'downvoted') downBtn.classList.add('active');
        }
    } catch (err) {
        console.error("Comment voting failed:", err);
    } finally {
        btnElement.disabled = false;
    }
}

function showReplyBox(parentId, authorName) {
    const container = document.getElementById(`reply-container-${parentId}`);

    if (container.innerHTML !== "") return;

    const html = `
        <div class="mt-2 mb-3">
            <textarea class="form-control form-control-sm mb-2" id="input-${parentId}" 
                      placeholder="Reply to u/${authorName}..."></textarea>
            <div class="d-flex gap-2">
                <button class="btn btn-sm btn-primary px-3" 
                        onclick="submitComment('${parentId}')">Reply</button>
                <button class="btn btn-sm btn-light" 
                        onclick="closeReplyBox('${parentId}')">Cancel</button>
            </div>
        </div>
    `;
    container.innerHTML = html;
}

function closeReplyBox(parentId) {
    document.getElementById(`reply-container-${parentId}`).innerHTML = "";
}

async function submitComment(parentId = null) {
    const postId = document.getElementById('postId').value;
    const inputId = parentId ? `input-${parentId}` : 'mainCommentInput';
    const inputElement = document.getElementById(inputId);
    const content = inputElement.value.trim();

    if (!content) return;

    // UI Feedback: Disable input while sending
    inputElement.disabled = true;

    try {
        const response = await fetch('/Comment/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ postId, content, parentId })
        });

        const contentType = response.headers.get("content-type");

        if (contentType && contentType.includes("application/json")) {
            const data = await response.json();
            if (data.loginUrl) {
                window.location.href = data.loginUrl;
                return;
            }
        }

        if (response.ok) {
            const htmlResponse = await response.text();

            const tempDiv = document.createElement('div');
            tempDiv.classList.add('newly-added-comment');
            tempDiv.innerHTML = htmlResponse;

            if (parentId) {
                const container = document.getElementById(`replies-container-${parentId}`);
                container.appendChild(tempDiv);
                closeReplyBox(parentId);
            } else {
                const container = document.getElementById('comments-container');
                container.prepend(tempDiv);
                inputElement.value = '';
            }
        } else {
            const errorMsg = await response.text();
            alert(errorMsg || "Error posting comment.");
        }
    } catch (err) {
        console.error("Critical error:", err);
    } finally {
        inputElement.disabled = false;
    }
}