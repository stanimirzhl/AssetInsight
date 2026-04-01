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


async function handleSortChange() {
    const sort = document.getElementById('commentSortSelect').value;
    document.getElementById('currentSort').value = sort;
    document.getElementById('currentPage').value = "1";
    document.getElementById('hasMore').value = "true";

    document.getElementById('comments-container').innerHTML = '';
    document.getElementById('status-message').classList.add('d-none');
    document.getElementById('loader').classList.remove('d-none');

    await loadMoreComments(true);
}

async function loadMoreComments(isReset = false) {
    if (isFetching) return;
    isFetching = true;

    const pageInput = document.getElementById('currentPage');
    const hasMoreInput = document.getElementById('hasMore');
    const sortBy = document.getElementById('currentSort').value;
    const postId = document.getElementById('postId').value;
    const loader = document.getElementById('loader');
    
    let nextPage = isReset ? 1 : parseInt(pageInput.value) + 1;

    try {
        const response = await fetch(`/Comment/GetMoreComments?postId=${postId}&pageIndex=${nextPage}&sortBy=${sortBy}`);

        if (response.status === 204) {
            hasMoreInput.value = "false";
            loader.classList.add('d-none');
            document.getElementById('status-message').classList.remove('d-none');
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
        console.error("Load more error:", err);
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
                //tempDiv.scrollIntoView({ behavior: 'smooth', block: 'start' });
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

function editComment(commentId) {
    const textDiv = document.querySelector(`#comment-body-wrapper-${commentId} .comment-text`);
    const actionsDiv = document.querySelector(`#comment-body-wrapper-${commentId} .comment-actions`);

    const originalContent = textDiv.innerText.trim();

    const editHtml = `
        <div class="edit-container mt-2">
            <textarea class="form-control mb-2" id="edit-input-${commentId}">${originalContent}</textarea>
            <div class="d-flex gap-2">
                <button class="btn btn-sm btn-success" onclick="saveEdit('${commentId}')">Save</button>
                <button class="btn btn-sm btn-outline-secondary" onclick="cancelEdit('${commentId}', \`${originalContent.replace(/`/g, '\\`')}\`)">Cancel</button>
            </div>
        </div>
    `;

    textDiv.style.display = 'none';
    actionsDiv.classList.add('d-none');
    textDiv.insertAdjacentHTML('afterend', editHtml);
}

function cancelEdit(commentId, originalContent) {
    const container = document.querySelector(`#comment-body-wrapper-${commentId} .edit-container`);
    const textDiv = document.querySelector(`#comment-body-wrapper-${commentId} .comment-text`);
    const actionsDiv = document.querySelector(`#comment-body-wrapper-${commentId} .comment-actions`);

    container.remove();
    textDiv.style.display = 'block';
    actionsDiv.style.display = 'flex';
    actionsDiv.classList.remove('d-none')
}

async function saveEdit(commentId) {
    const newText = document.getElementById(`edit-input-${commentId}`).value;
    const postId = document.getElementById('postId').value;

    const response = await fetch(`/Comment/Edit/${commentId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        },        
        body: JSON.stringify({ content: newText, postId: postId })
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
        const textDiv = document.querySelector(`#comment-body-wrapper-${commentId} .comment-text`);
        textDiv.innerText = newText;
        cancelEdit(commentId);
    } else {
        alert("Failed to save changes. Please try again.");
    }
}

async function deleteComment(commentId) {
    if (!confirm("Are you sure you want to delete this comment?")) return;

    const postId = document.getElementById('postId').value;

    const response = await fetch(`/Comment/Delete?commentId=${commentId}&postId=${postId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        }
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
        const wrapper = document.getElementById(`comment-body-wrapper-${commentId}`);

        const textDiv = wrapper.querySelector('.comment-text');
        const actionsDiv = wrapper.querySelector('.comment-actions');

        const loadRepliesBtn = actionsDiv.querySelector('.load-replies-btn');

        textDiv.innerHTML = '<span class="text-muted fst-italic"><i class="bi bi-trash"></i> [Comment deleted]</span>';


        actionsDiv.innerHTML = '';

        if (loadRepliesBtn) {
            actionsDiv.appendChild(loadRepliesBtn);
        }

        wrapper.classList.add('opacity-75');
    }
}

const btn = document.querySelector('.js-save-btn');

if (btn) {
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
                updateSaveButtonUI(btn, data.saved);
                showToast(data.message);
            }
        } catch (error) {
            console.error('Error:', error);
        }
    });
    function updateSaveButtonUI(btn, isSaved) {
        const textSpan = btn.querySelector('.save-text');
        const icon = btn.querySelector('i');

        if (isSaved) {
            btn.classList.add('active');
            if (textSpan) textSpan.textContent = 'Saved';
            if (icon) icon.className = 'bi bi-bookmark-fill';
        } else {
            btn.classList.remove('active');
            if (textSpan) textSpan.textContent = 'Save';
            if (icon) icon.className = 'bi bi-bookmark';
        }
    }
}