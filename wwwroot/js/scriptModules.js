export function resizePlayerDeck() {
    var playerDeck = document.querySelector('#player-deck');
    var sidebarWidth = document.querySelector('.sidebar').offsetWidth;
    sidebarWidth = sidebarWidth <= 250 ? 250 : 0;
    var a = Math.max(0, (1 - (window.innerWidth / 1200)) * 75);
    var size = (window.innerWidth - (sidebarWidth + 60) - a) / (playerDeck?.childElementCount ?? 0);
    for (var el of playerDeck?.children ?? []) {
        el.style.margin = `5px -${(130 - size) / 2}px`;
    }
}

export function attachPlayerDeckEvent() {
    window.onresize = () => resizePlayerDeck();
}

export function dettachResizePlayerDeckEvent() {
    window.onresize = null;
}

export function scrollChat() {
    setTimeout(() => document.querySelector('.chat-list')?.lastElementChild.scrollIntoView(false), 20);
}

export function attachFullscreenEvent() {
    document.onfullscreenchange = () => {
        var fullscreenButton = document.querySelector('#fullscreen-button');
        if (document.fullscreen) {
            fullscreenButton.classList.replace('oi-fullscreen-enter', 'oi-fullscreen-exit');
        }
        else {
            fullscreenButton.classList.replace('oi-fullscreen-exit', 'oi-fullscreen-enter');
        }
    };
}

export function dettachFullscreenEvent() {
    document.onfullscreenchange = null;
}

export function toggleFullscreen() {
    if (document.fullscreen) {
        document.exitFullscreen()
    }
    else {
        document.documentElement.requestFullscreen()
    }
}