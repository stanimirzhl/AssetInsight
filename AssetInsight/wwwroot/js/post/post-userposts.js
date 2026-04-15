const btn = document.querySelector('.follow-btn');
let culture = "en";

if (btn) {
    btn.addEventListener('click', async function (event) {
        event.preventDefault();

        try {
            culture = await getCurrentCulture();
        } catch (e) {
            culture = "en";
        }

        const userName = btn.dataset.userName;
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        try {
            const response = await fetch(`/Follow/Follow?userName=${userName}`, {
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
                updateFollowButtonUI(btn, data.isFollowing);
                showToast(data.message);
            }
        } catch (error) {
            console.error('Error:', error);
        }
    });

    function updateFollowButtonUI(btn, isFollowing) {
        const t = followTranslations[culture] || followTranslations.en;

        if (isFollowing) {
            btn.classList.add('active');
            if (btn) btn.textContent = t.following;
        } else {
            btn.classList.remove('active');
            if (btn) btn.textContent = t.follow;
        }
    }

    const followTranslations = {
        en: { follow: "Follow", following: "Following" },
        bg: { follow: "Последвай", following: "Следваш" },
        de: { follow: "Folgen", following: "Folge ich" },
        es: { follow: "Seguir", following: "Siguiendo" },
        fr: { follow: "Suivre", following: "Suivi" }
    };
}