const connection = new signalR.HubConnectionBuilder()
    .withUrl('/gamehub')
    .withAutomaticReconnect()
    .build();

let mySymbol = null;
let board = Array(9).fill(null);
let gameOver = false;

const statusEl = document.getElementById('status');
const youEl = document.getElementById('you');
const boardEl = document.getElementById('board');
const restartBtn = document.getElementById('restart');

function setStatus(msg, cls = '') {
    statusEl.textContent = msg;
    statusEl.className = cls;
}

function render(winLine = []) {
    boardEl.innerHTML = '';
    board.forEach((val, i) => {
        const cell = document.createElement('div');
        cell.className = 'cell' +
            (val ? ` taken ${val.toLowerCase()}` : '') +
            (winLine.includes(i) ? ' winning' : '') +
            (gameOver || val ? ' inactive' : '');
        cell.textContent = val ?? '';
        cell.addEventListener('click', () => handleClick(i));
        boardEl.appendChild(cell);
    });
}

function handleClick(i) {
    if (gameOver || board[i] || !mySymbol) return;
    connection.invoke('MakeMove', i).catch(console.error);
}

connection.on('Waiting', () => {
    setStatus('Waiting for an opponent…');
    render();
});

connection.on('GameStarted', (symbol) => {
    mySymbol = symbol;
    board = Array(9).fill(null);
    gameOver = false;
    youEl.textContent = `You are ${symbol}`;
    setStatus(`${symbol === 'X' ? "Your" : "Opponent's"} turn — X goes first`);
    restartBtn.style.display = 'none';
    render();
});

connection.on('BoardUpdated', (newBoard, currentTurn) => {
    board = newBoard;
    const yourTurn = currentTurn === mySymbol;
    setStatus(yourTurn ? 'Your turn' : "Opponent's turn");
    render();
});

connection.on('GameOver', (newBoard, winner, winLine) => {
    board = newBoard;
    gameOver = true;
    if (winner) {
        setStatus(winner === mySymbol ? 'You win!' : 'You lose!', 'winner');
    } else {
        setStatus("It's a draw!", 'draw');
    }
    render(winLine ?? []);
    restartBtn.style.display = '';
});

connection.on('OpponentDisconnected', () => {
    gameOver = true;
    setStatus('Opponent disconnected.', 'disconnected');
    restartBtn.style.display = '';
});

restartBtn.addEventListener('click', () => {
    restartBtn.style.display = 'none';
    mySymbol = null;
    setStatus('Finding a new game…');
    connection.invoke('JoinQueue').catch(console.error);
});

connection.start()
    .then(() => connection.invoke('JoinQueue'))
    .catch(err => setStatus('Connection failed.', 'disconnected'));
