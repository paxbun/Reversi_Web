var body;
var front_message;
var front_message_inner;
var content;
var head;
var player;
var etc;
var score;
var space;
var space_indices;
var space_grid;
var space_button_grid;
var space_button_grid_buttons = [];
var div_buttons;

var piece_state = [];

var current_player = 1;
var dark_count = 2;
var light_count = 2;

var socket;

const dark_player_str = 'Dark\'s turn';
const light_player_str = 'Light\'s turn';
const dark_player = 1;
const light_player = 2;
const dark_won = 3;
const dark_won_auto = 4;
const light_won = 5;
const light_won_auto = 6;
const draw = 7;
const draw_auto = 8;
const no_available_spaces = 10;
const player_str = [
    "",
    "Dark's turn",
    "Light's turn",
    'Dark won <a onclick="restart()">Restart</a>',
    'Dark won',
    'Light won <a onclick="restart()">Restart</a>',
    'Light won',
    'Draw <a onclick="restart()">Restart</a>',
    'Draw',
    "",
    'No spaces <a onclick="skip_turn()">Skip</a>'
];
const placeable = 3;

var initialized = false;
var established = false;

function getElem(str) {
    return document.getElementById(str);
}

function space_button_grid_button_click() {
    if (initialized) {
        socket.send('PLCE' + (this.x) + (this.y));
    }
}

function skip_turn() {
    if (initialized) {
        socket.send('SKIP');
    }
}

function close() {
    body.insertBefore(front_message, body.childNodes[0]);
    front_message.innerHTML = front_message_inner;
    initialized = false;
}

function restart() {
    if (initialized) {
        socket.send('RSTT');
        close();
    }
}

function start_init(mode) {
    if (established) {
        socket.send('INIT' + String.fromCharCode(mode));
    }
}


function update_piece() {
    for(var i = 0; i < 64; i++) {
        space_button_grid_buttons[i].innerHTML = '';
        switch (piece_state[i]) {
            case dark_player:
                space_button_grid_buttons[i].innerHTML = '<div class="dark-piece"></div>'
                break;
            case light_player:
                space_button_grid_buttons[i].innerHTML = '<div class="light-piece"></div>'
                break;
            case placeable:
                space_button_grid_buttons[i].innerHTML = '<div class="placeable"></div>'
                break;
        }
    }
    player.innerHTML = player_str[current_player];
    score.innerHTML = 'Dark: ' + dark_count + '  Light: ' + light_count;
}

function initialize() {
    socket = new WebSocket('ws://' + window.location.hostname + ':5567/Reversi');
    socket.onmessage = function (event) {
        console.log(event.data);
        var command = new String(event.data).substr(0, 4);
        var argument = new String(event.data).substr(4, event.data.length);
        switch (command) {
            case 'INIT':
                Array.prototype.forEach.call(div_buttons, function (div_button) {
                    front_message.removeChild(div_button);
                });
                front_message.innerHTML += '<div class="div-button-disabled use-text">Waiting for response...</div>';
                break;
            case 'STRT':
                initialized = true;
                body.removeChild(front_message); 
                break;
            case 'OBSV':
                initialized = false;
                body.removeChild(front_message);
                break;
            case 'CLSE':
                close();
                break;
            case 'UPDT':
                for (var i = 0; i < 64; i++) {
                    piece_state[i] = argument.charCodeAt(i);
                }
                current_player = argument.charCodeAt(64);
                dark_count = argument.charCodeAt(65);
                light_count = argument.charCodeAt(66);
                update_piece();
                break;
            case 'ERRA':
                etc.innerHTML = '<span style="color: red">' + argument + '</span>';
                break;
            case 'PRNT':
                etc.innerHTML = argument;
                break;
        }
    }
    socket.onopen = function (event) {
        etc.innerHTML = 'Connection established';
        established = true;
    }
    socket.onclose = function (event) {
        etc.innerHTML = 'Disconnected';
        body.insertBefore(front_message, body.childNodes[0]);
        window.close();
        initialized = false;
        established = false;
    }
}

window.onload = function() {
    body = document.getElementsByTagName('body')[0];
    front_message = getElem('front-message');
    front_message_inner = front_message.innerHTML;
    content = getElem('content');
    head = getElem('head');
    player = getElem('player');
    score = getElem('score');
    etc = getElem('etc');
    space = getElem('space');
    space_indices = getElem('space-indices');
    space_grid = getElem('space-grid');
    space_button_grid = getElem('space-button-grid');
    div_buttons = document.getElementsByClassName('div-button');
    
    for(var i = 0; i < 64; i++) {
        piece_state.push(0);
    }
    
    const grid_button_item_str = '<div class="grid-button-item"></div>'
    // create button grid
    for(var i = 0; i < 8; i++) {
        for (var j = 0; j < 8; j++) {
            space_button_grid.innerHTML += grid_button_item_str;
        }
    }
    space_button_grid_buttons = space_button_grid.childNodes;
    for(var i = 0; i < 8; i++) {
        for(var j = 0; j < 8; j++) {
            space_button_grid_buttons[i*8 + j].x = j;
            space_button_grid_buttons[i*8 + j].y = i;
            space_button_grid_buttons[i*8 + j].onclick =
                space_button_grid_button_click;
        }
    }

    const grid_item_str = '<div class="grid-item"></div>'
    // draw grid
    for(var i = 0; i < 64; i++) {
        space_grid.innerHTML += grid_item_str;
    }

    // draw indices
    for(var i = 0; i < 8; i++) {
        space_indices.innerHTML += '<div class="grid-indices-item use-text" style="left: ' + (40 * (i+1)) + 'px">' + String.fromCharCode(i + 65) + '</div>'
        space_indices.innerHTML += '<div class="grid-indices-item use-text" style="top: ' + (40 * i + 53) + 'px">' + (i + 1) + '</div>'
    }

    update_piece();
    player.innerHTML = dark_player_str;

    initialize();
}