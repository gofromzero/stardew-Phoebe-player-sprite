const assert = require('node:assert/strict');
const fs = require('node:fs');
const html = fs.readFileSync('preview.html', 'utf8');
new Function(html.match(/<script>([\s\S]*?)<\/script>/)[1]);
const body = html.match(/function drawFrame\([^]*?\nlet last=/)[0].replace(/\nlet last=$/, '');
const calls=[], frames=Array.from({length:16},(_,i)=>i+100), s={value:'walk'};
const draw = new Function('frames','cols','s','ctx',body+';return drawFrame;')(frames,4,s,{drawImage:(...a)=>calls.push(a)});
for(let row=0;row<4;row++)for(let col=0;col<4;col++){
 calls.length=0;draw(row*4+col,120,10,240);
 assert.deepEqual(calls[0],[frames[row*4],0,0,40,34,120,10,240,204]);
 assert.deepEqual(calls[1],[frames[row*4+col],0,34,40,6,120,214,240,36]);
}
s.value='tools';calls.length=0;draw(2,0,0,80);assert.deepEqual(calls,[[frames[2],0,0,80,80]]);
console.log('All 16 walk frames share the upper body within each direction; feet cover remaining rows; other actions unchanged.');
