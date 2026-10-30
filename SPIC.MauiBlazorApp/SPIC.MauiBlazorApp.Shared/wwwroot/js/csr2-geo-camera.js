// Deploy to wwwroot/js/csr2-geo-camera.js in the Blazor web project.
// Requires HTTPS, camera and location permission. Does not permit gallery selection.
let active = null;
function locate() {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) return reject(new Error('GPS is unavailable.'));
    navigator.geolocation.getCurrentPosition(resolve, reject,
      {enableHighAccuracy:true, timeout:20000, maximumAge:0});
  });
}
function textOverlay(ctx, width, height, coords, venue) {
  const font = Math.max(17, Math.round(width/42));
  const lines = [
    `GPS ${coords.latitude.toFixed(6)}, ${coords.longitude.toFixed(6)}  (+/-${Math.round(coords.accuracy)}m)`,
    new Date().toLocaleString(),
    venue ? `Venue: ${venue.slice(0,90)}` : 'SPIC CSR-2 event'
  ];
  ctx.font = `bold ${font}px sans-serif`;
  const padding = Math.round(font*.65), h = lines.length*(font+padding)+padding;
  ctx.fillStyle='rgba(0,0,0,.72)'; ctx.fillRect(0,height-h,width,h);
  ctx.fillStyle='#fff'; ctx.textBaseline='top';
  lines.forEach((line,i) => ctx.fillText(line,padding,height-h+padding+i*(font+padding),width-padding*2));
}
function emitFile(id, blob, name) {
  const input=document.getElementById(id);
  if(!input) throw new Error('Camera input not available in the current form.');
  const dt=new DataTransfer(); dt.items.add(new File([blob],name,{type:blob.type}));
  input.files=dt.files;
  input.dispatchEvent(new Event('change',{bubbles:true}));
}
export async function openGeoCamera(isVideo, venue) {
  if(active) throw new Error('A camera session is already running.');
  const mobile = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent) || navigator.userAgentData?.mobile;
  if(!mobile) throw new Error('CSR-2 event capture is available only on mobile devices.');
  if(!window.isSecureContext) throw new Error('Camera and GPS require HTTPS or a secure app WebView.');
  if(!navigator.mediaDevices?.getUserMedia) throw new Error('Live camera is unavailable in this browser/WebView.');
  const pos=await locate(); // no fallback to missing or fabricated location
  const gps=pos.coords;
  const stream=await navigator.mediaDevices.getUserMedia({video:{facingMode:{ideal:'environment'}},audio:!!isVideo});
  const backdrop=document.createElement('div');
  backdrop.style.cssText='position:fixed;inset:0;background:#09121d;z-index:2147483000;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:14px;padding:12px;color:white';
  const preview=document.createElement('video');preview.autoplay=true;preview.muted=true;preview.playsInline=true;
  preview.srcObject=stream; preview.style.cssText='max-width:100%;max-height:70vh;object-fit:contain';
  const gpsLabel=document.createElement('div');gpsLabel.textContent=`GPS: ${gps.latitude.toFixed(6)}, ${gps.longitude.toFixed(6)} (+/- ${Math.round(gps.accuracy)}m)`;
  const status=document.createElement('div');status.textContent='GPS acquired. Ready to capture.';
  const buttons=document.createElement('div');buttons.style.cssText='display:flex;gap:10px';
  const capture=document.createElement('button');capture.textContent=isVideo?'Start Recording':'Capture Photo';
  const cancel=document.createElement('button');cancel.textContent='Cancel';
  [capture,cancel].forEach(b=>{b.type='button';b.style.cssText='border:0;border-radius:8px;padding:12px 18px;font-weight:bold;cursor:pointer'});
  buttons.append(capture,cancel);backdrop.append(preview,gpsLabel,status,buttons);document.body.append(backdrop);
  let recorder=null, paintId=0, canvasStream=null, complete=false;
  function finish(){if(complete)return; complete=true;cancelAnimationFrame(paintId);stream.getTracks().forEach(t=>t.stop());canvasStream?.getTracks().forEach(t=>t.stop());backdrop.remove();active=null;}
  active={finish};
  cancel.onclick=()=>{if(recorder?.state==='recording')recorder.stop();finish();};
  try{await preview.play();}catch{}
  capture.onclick=async()=>{
    try {
      const w=Math.max(640,preview.videoWidth||1280),h=Math.max(480,preview.videoHeight||720);
      const canvas=document.createElement('canvas');canvas.width=w;canvas.height=h;
      const ctx=canvas.getContext('2d',{alpha:false});
      function paint(){ctx.drawImage(preview,0,0,w,h);textOverlay(ctx,w,h,gps,venue);}
      if(!isVideo){
        paint();const blob=await new Promise(r=>canvas.toBlob(r,'image/jpeg',.88));
        if(!blob)throw new Error('Unable to create photo.');
        emitFile('dmtp-photos-camera',blob,`CSR2_GPS_${Date.now()}.jpg`);finish();return;
      }
      if(!window.MediaRecorder || !canvas.captureStream)throw new Error('Video watermark recording is not supported on this device.');
      const types=['video/mp4','video/webm;codecs=vp8,opus','video/webm'];
      const mime=types.find(t=>MediaRecorder.isTypeSupported(t));
      if(!mime)throw new Error('No supported recording format.');
      paint();canvasStream=canvas.captureStream(20);
      // Include microphone audio when available.
      const tracks=[...canvasStream.getVideoTracks(),...stream.getAudioTracks()];
      recorder=new MediaRecorder(new MediaStream(tracks),{mimeType:mime});
      const parts=[];
      recorder.ondataavailable=e=>{if(e.data.size)parts.push(e.data)};
      recorder.onerror=e=>{status.textContent='Recording failed: '+(e.error?.message||'unknown error')};
      recorder.onstop=()=>{
        if(complete)return;
        const type=mime.startsWith('video/mp4')?'video/mp4':'video/webm';
        const blob=new Blob(parts,{type});
        if(blob.size>700*1024*1024){status.textContent='Recording exceeds 700 MB. Please record a shorter video.';finish();return;}
        try{emitFile('dmtp-videos-record',blob,`CSR2_GPS_${Date.now()}.${type==='video/mp4'?'mp4':'webm'}`)}catch(e){alert(e.message)}
        finish();
      };
      function frame(){if(complete)return;paint();paintId=requestAnimationFrame(frame)}frame();
      recorder.start(1000); capture.textContent='Stop & Save Video';status.textContent='Recording with location watermark...';
      capture.onclick=()=>{if(recorder.state==='recording')recorder.stop();capture.disabled=true};
    }catch(e){status.textContent=e.message||String(e)}
  };
}
