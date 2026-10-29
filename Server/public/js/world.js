// The 3D background of the website: one long scene that the camera flies through while the page scrolls.
// Hero core -> Code City -> Physics Valley -> Chemical Chaos -> Bio-Zone -> The Core -> Exam Hall -> install core.
import * as THREE from '../vendor/three/three.module.min.js';
import { EffectComposer } from '../vendor/three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from '../vendor/three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from '../vendor/three/addons/postprocessing/UnrealBloomPass.js';
import { OutputPass } from '../vendor/three/addons/postprocessing/OutputPass.js';

const SPACING = 40;                       // distance between worlds along -z
const WZ = (i) => -SPACING * (i + 1);     // world i centre (0 = Code City)
const END_Z = WZ(6) - 20;                 // the install core after the exam hall

// 3D simplex noise (Ashima Arts / Stefan Gustavson, MIT)
const NOISE = /* glsl */`
vec3 mod289(vec3 x){return x-floor(x*(1.0/289.0))*289.0;}
vec4 mod289(vec4 x){return x-floor(x*(1.0/289.0))*289.0;}
vec4 permute(vec4 x){return mod289(((x*34.0)+1.0)*x);}
vec4 taylorInvSqrt(vec4 r){return 1.79284291400159-0.85373472095314*r;}
float snoise(vec3 v){
  const vec2 C=vec2(1.0/6.0,1.0/3.0); const vec4 D=vec4(0.0,0.5,1.0,2.0);
  vec3 i=floor(v+dot(v,C.yyy)); vec3 x0=v-i+dot(i,C.xxx);
  vec3 g=step(x0.yzx,x0.xyz); vec3 l=1.0-g; vec3 i1=min(g.xyz,l.zxy); vec3 i2=max(g.xyz,l.zxy);
  vec3 x1=x0-i1+C.xxx; vec3 x2=x0-i2+C.yyy; vec3 x3=x0-D.yyy;
  i=mod289(i);
  vec4 p=permute(permute(permute(i.z+vec4(0.0,i1.z,i2.z,1.0))+i.y+vec4(0.0,i1.y,i2.y,1.0))+i.x+vec4(0.0,i1.x,i2.x,1.0));
  float n_=0.142857142857; vec3 ns=n_*D.wyz-D.xzx;
  vec4 j=p-49.0*floor(p*ns.z*ns.z); vec4 x_=floor(j*ns.z); vec4 y_=floor(j-7.0*x_);
  vec4 x=x_*ns.x+ns.yyyy; vec4 y=y_*ns.x+ns.yyyy; vec4 h=1.0-abs(x)-abs(y);
  vec4 b0=vec4(x.xy,y.xy); vec4 b1=vec4(x.zw,y.zw);
  vec4 s0=floor(b0)*2.0+1.0; vec4 s1=floor(b1)*2.0+1.0; vec4 sh=-step(h,vec4(0.0));
  vec4 a0=b0.xzyw+s0.xzyw*sh.xxyy; vec4 a1=b1.xzyw+s1.xzyw*sh.zzww;
  vec3 p0=vec3(a0.xy,h.x); vec3 p1=vec3(a0.zw,h.y); vec3 p2=vec3(a1.xy,h.z); vec3 p3=vec3(a1.zw,h.w);
  vec4 norm=taylorInvSqrt(vec4(dot(p0,p0),dot(p1,p1),dot(p2,p2),dot(p3,p3)));
  p0*=norm.x; p1*=norm.y; p2*=norm.z; p3*=norm.w;
  vec4 m=max(0.6-vec4(dot(x0,x0),dot(x1,x1),dot(x2,x2),dot(x3,x3)),0.0); m=m*m;
  return 42.0*dot(m*m,vec4(dot(p0,x0),dot(p1,x1),dot(p2,x2),dot(p3,x3)));
}`;

const rand = mulberry(7);
function mulberry(a) { return () => { a |= 0; a = (a + 0x6D2B79F5) | 0; let t = Math.imul(a ^ (a >>> 15), 1 | a); t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t; return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }
const rr = (a, b) => a + (b - a) * rand();
const smooth = (t) => t * t * (3 - 2 * t);

export function startWorld(canvas, opts = {}) {
  const low = !!opts.low;
  const reduced = !!opts.reducedMotion;
  let renderer;
  try {
    renderer = new THREE.WebGLRenderer({ canvas, antialias: !low, powerPreference: 'high-performance' });
  } catch (e) { return null; }
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, low ? 1.25 : 1.75));
  renderer.setSize(window.innerWidth, window.innerHeight, false);
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 0.9;

  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(0x05060d, 0.04);
  scene.background = new THREE.Color(0x05060d);
  const camera = new THREE.PerspectiveCamera(55, window.innerWidth / window.innerHeight, 0.1, 400);
  camera.position.set(0, 0.3, 8);

  const composer = new EffectComposer(renderer);
  composer.addPass(new RenderPass(scene, camera));
  const bloom = new UnrealBloomPass(new THREE.Vector2(window.innerWidth, window.innerHeight), low ? 0.45 : 0.6, 0.45, 0.55);
  if (low) bloom.resolution.set(window.innerWidth / 2, window.innerHeight / 2);
  composer.addPass(bloom);
  composer.addPass(new OutputPass());

  scene.add(new THREE.HemisphereLight(0x8fbfff, 0x10051a, 0.55));
  const sun = new THREE.DirectionalLight(0xffffff, 0.9);
  sun.position.set(5, 12, 6);
  scene.add(sun);

  const updaters = [];        // (t, dt) => void
  const uniforms = { uTime: { value: 0 }, uCharge: { value: 0 }, uBurst: { value: 0 } };

  // ---------------------------------------------------------------- shared: floor grid, dust, data streams
  {
    const len = -END_Z + 80;
    const g = new THREE.PlaneGeometry(160, len, 1, 1);
    g.rotateX(-Math.PI / 2);
    g.translate(0, 0, -len / 2 + 30);
    const m = new THREE.ShaderMaterial({
      transparent: true, depthWrite: false, fog: true,
      uniforms: Object.assign({ uColorA: { value: new THREE.Color(0x40ebff) }, uColorB: { value: new THREE.Color(0xff3dc8) } }, uniforms, THREE.UniformsUtils.clone(THREE.UniformsLib.fog)),
      vertexShader: `
        varying vec3 vW;
        #include <fog_pars_vertex>
        void main() {
          vec4 w = modelMatrix * vec4(position, 1.0);
          vW = w.xyz;
          vec4 mvPosition = viewMatrix * w;
          gl_Position = projectionMatrix * mvPosition;
          #include <fog_vertex>
        }`,
      fragmentShader: `
        uniform float uTime; uniform vec3 uColorA; uniform vec3 uColorB;
        varying vec3 vW;
        #include <fog_pars_fragment>
        void main() {
          vec2 p = vW.xz;
          vec2 g = abs(fract(p / 2.0 - 0.5) - 0.5) / fwidth(p / 2.0);
          float line = 1.0 - min(min(g.x, g.y), 1.0);
          vec2 G = abs(fract(p / 10.0 - 0.5) - 0.5) / fwidth(p / 10.0);
          float major = 1.0 - min(min(G.x, G.y), 1.0);
          float pulse = smoothstep(0.96, 1.0, fract(vW.z * 0.025 + uTime * 0.25));
          vec3 col = mix(uColorA, uColorB, 0.5 + 0.5 * sin(vW.z * 0.03 + uTime * 0.3));
          float fade = smoothstep(60.0, 8.0, abs(vW.x));
          gl_FragColor = vec4(col * (line * 0.35 + major * 0.9 + pulse * 0.8), (line * 0.35 + major * 0.7 + pulse * 0.6) * fade);
          #include <fog_fragment>
        }`,
    });
    const floor = new THREE.Mesh(g, m);
    floor.position.y = -3;
    scene.add(floor);

    const N = low ? 1500 : 4000;
    const pos = new Float32Array(N * 3);
    for (let i = 0; i < N; i++) { pos[i * 3] = rr(-50, 50); pos[i * 3 + 1] = rr(-3, 30); pos[i * 3 + 2] = rr(END_Z - 30, 30); }
    const dg = new THREE.BufferGeometry();
    dg.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    const dust = new THREE.Points(dg, new THREE.PointsMaterial({ color: 0x9fdcff, size: 0.05, transparent: true, opacity: 0.45, depthWrite: false, blending: THREE.AdditiveBlending }));
    scene.add(dust);
    updaters.push((t) => { dust.rotation.y = Math.sin(t * 0.02) * 0.02; });
  }

  // ---------------------------------------------------------------- the glitch core (hero + install)
  function makeCore(radius, at) {
    const grp = new THREE.Group();
    grp.position.copy(at);
    const mat = new THREE.ShaderMaterial({
      uniforms: Object.assign({ uSeed: { value: rand() * 10 } }, uniforms),
      vertexShader: NOISE + `
        uniform float uTime; uniform float uCharge; uniform float uBurst; uniform float uSeed;
        varying vec3 vN; varying vec3 vV; varying float vD;
        void main(){
          float n=snoise(normal*1.6+vec3(uTime*0.35+uSeed));
          float glitch=step(0.93,fract(sin(floor(uTime*8.0)*12.9898+position.y*3.0)*43758.5453))*0.25;
          float d=n*(0.16+uCharge*0.42+uBurst*0.8)+glitch*(0.3+uCharge);
          vD=d;
          vec3 p=position+normal*d;
          vec4 mv=modelViewMatrix*vec4(p,1.0);
          vN=normalize(normalMatrix*normal); vV=normalize(-mv.xyz);
          gl_Position=projectionMatrix*mv;
        }`,
      fragmentShader: `
        uniform float uTime; uniform float uCharge; varying vec3 vN; varying vec3 vV; varying float vD;
        void main(){
          float f=pow(1.0-max(dot(vN,vV),0.0),2.2);
          vec3 cyan=vec3(0.25,0.92,1.0); vec3 mag=vec3(1.0,0.24,0.78);
          vec3 col=mix(vec3(0.02,0.04,0.12),mix(cyan,mag,smoothstep(-0.2,0.35,vD)),0.35+f*0.9);
          float bands=smoothstep(0.92,1.0,sin(vD*40.0+uTime*3.0));
          col+=bands*mag*0.6+f*cyan*(0.6+uCharge*1.6);
          gl_FragColor=vec4(col*(1.0+uCharge*0.8),1.0);
        }`,
    });
    const core = new THREE.Mesh(new THREE.IcosahedronGeometry(radius, low ? 24 : 48), mat);
    grp.add(core);
    const wire = new THREE.LineSegments(new THREE.WireframeGeometry(new THREE.IcosahedronGeometry(radius * 1.45, 1)),
      new THREE.LineBasicMaterial({ color: 0x40ebff, transparent: true, opacity: 0.35, blending: THREE.AdditiveBlending }));
    grp.add(wire);
    const rings = [];
    for (let i = 0; i < 3; i++) {
      const r = new THREE.Mesh(new THREE.TorusGeometry(radius * (1.75 + i * 0.32), 0.012 + i * 0.006, 8, 160),
        new THREE.MeshBasicMaterial({ color: i === 1 ? 0xff3dc8 : 0x40ebff, transparent: true, opacity: 0.9 }));
      r.rotation.set(rr(0, Math.PI), rr(0, Math.PI), 0);
      rings.push(r); grp.add(r);
    }
    const SH = low ? 50 : 110;
    const shards = new THREE.InstancedMesh(new THREE.TetrahedronGeometry(0.09), new THREE.MeshBasicMaterial({ color: 0xffffff }), SH);
    const sd = [];
    for (let i = 0; i < SH; i++) {
      sd.push({ r: radius * rr(1.9, 3.4), a: rr(0, Math.PI * 2), y: rr(-1, 1) * radius * 0.9, s: rr(0.25, 0.9) * (rand() < 0.5 ? 1 : -1), k: rr(0.5, 1.6) });
      shards.setColorAt(i, new THREE.Color(rand() < 0.5 ? 0x40ebff : 0xff3dc8));
    }
    grp.add(shards);
    const light = new THREE.PointLight(0x40ebff, 12, 20, 1.8);
    grp.add(light);
    scene.add(grp);
    const m4 = new THREE.Matrix4(), q = new THREE.Quaternion(), e = new THREE.Euler(), v = new THREE.Vector3(), sc = new THREE.Vector3();
    updaters.push((t, dt) => {
      const c = uniforms.uCharge.value, b = uniforms.uBurst.value;
      const spin = 1 + c * 6;
      core.rotation.y += dt * 0.15 * spin; core.rotation.x += dt * 0.05 * spin;
      wire.rotation.y -= dt * 0.1 * spin; wire.rotation.z += dt * 0.04 * spin;
      rings.forEach((r, i) => { r.rotation.x += dt * (0.2 + i * 0.13) * spin; r.rotation.y += dt * (0.12 - i * 0.05) * spin; });
      grp.scale.setScalar(1 + c * 0.18 + b * 0.25);
      light.intensity = 12 + c * 60 + b * 160;
      for (let i = 0; i < SH; i++) {
        const d = sd[i];
        d.a += dt * d.s * spin * 0.6;
        const rad = d.r * (1 - c * 0.35) + b * 6 * d.k;
        v.set(Math.cos(d.a) * rad, d.y + Math.sin(t * d.k + i) * 0.2, Math.sin(d.a) * rad);
        e.set(t * d.k, t * d.s, 0); q.setFromEuler(e); sc.setScalar(1 + c);
        m4.compose(v, q, sc); shards.setMatrixAt(i, m4);
      }
      shards.instanceMatrix.needsUpdate = true;
    });
    return grp;
  }
  makeCore(1.25, new THREE.Vector3(4.4, 0.2, -1));
  makeCore(1.6, new THREE.Vector3(0, 0.6, END_Z));

  // ---------------------------------------------------------------- 1. Code City
  {
    const z0 = WZ(0);
    const N = low ? 90 : 170;
    const mat = new THREE.ShaderMaterial({
      uniforms, fog: false,
      vertexShader: `varying vec3 vL; varying float vSeed; varying float vH;
        void main(){
          vec4 w=vec4(position,1.0);
          #ifdef USE_INSTANCING
          w=instanceMatrix*w; vSeed=fract(sin(dot(instanceMatrix[3].xz,vec2(12.9898,78.233)))*43758.5453);
          vH=length(instanceMatrix[1].xyz);
          #endif
          vL=position*vec3(1.0,vH,1.0);
          gl_Position=projectionMatrix*viewMatrix*modelMatrix*w;
        }`,
      fragmentShader: `uniform float uTime; varying vec3 vL; varying float vSeed; varying float vH;
        float h(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
        void main(){
          vec2 uv=vec2(vL.x+vL.z,vL.y)*vec2(3.0,2.2);
          vec2 id=floor(uv); vec2 f=fract(uv);
          float win=step(0.18,f.x)*step(f.x,0.82)*step(0.2,f.y)*step(f.y,0.75);
          float lit=step(0.55,h(id+vSeed*31.0+floor(uTime*0.2+vSeed*7.0)*0.0));
          float flick=0.75+0.25*sin(uTime*3.0+h(id)*30.0);
          vec3 c=mix(vec3(0.25,0.92,1.0),vec3(1.0,0.24,0.78),step(0.7,h(id*1.7+vSeed)));
          vec3 base=vec3(0.02,0.03,0.07)+vec3(0.0,0.02,0.05)*(vL.y/(vH+0.01));
          float top=smoothstep(vH*0.5-0.08,vH*0.5,vL.y);
          vec3 col=base+c*win*lit*flick*1.3+top*c*0.8;
          float d=gl_FragCoord.z/gl_FragCoord.w; col=mix(col,vec3(0.02,0.04,0.09),clamp(1.0-exp(-0.0012*d*d),0.0,1.0));
          gl_FragColor=vec4(col,1.0);
        }`,
    });
    const towers = new THREE.InstancedMesh(new THREE.BoxGeometry(1, 1, 1), mat, N);
    const m4 = new THREE.Matrix4();
    let n = 0;
    while (n < N) {
      const x = rr(-26, 26), z = z0 + rr(-22, 18);
      if (Math.abs(x) < 3.2) continue;
      const h = rr(3, 18) * (Math.abs(x) > 12 ? 1.4 : 1);
      const w = rr(1.2, 3.2), d = rr(1.2, 3.2);
      m4.compose(new THREE.Vector3(x, -3 + h / 2, z), new THREE.Quaternion(), new THREE.Vector3(w, h, d));
      towers.setMatrixAt(n++, m4);
    }
    scene.add(towers);

    const C = low ? 26 : 60;
    const cars = new THREE.InstancedMesh(new THREE.BoxGeometry(0.9, 0.08, 0.08), new THREE.MeshBasicMaterial({ color: 0xffffff }), C);
    const cd = [];
    for (let i = 0; i < C; i++) {
      cd.push({ y: rr(1, 9), z: z0 + rr(-16, 12), s: rr(4, 12) * (rand() < 0.5 ? 1 : -1), x: rr(-30, 30) });
      cars.setColorAt(i, new THREE.Color(rand() < 0.5 ? 0x40ebff : 0xff3dc8).multiplyScalar(2));
    }
    scene.add(cars);
    const v = new THREE.Vector3(), q = new THREE.Quaternion(), s1 = new THREE.Vector3(1, 1, 1);
    updaters.push((t, dt) => {
      for (let i = 0; i < C; i++) {
        const c = cd[i]; c.x += c.s * dt; if (c.x > 32) c.x = -32; if (c.x < -32) c.x = 32;
        v.set(c.x, c.y, c.z); m4.compose(v, q, s1); cars.setMatrixAt(i, m4);
      }
      cars.instanceMatrix.needsUpdate = true;
    });
    const l = new THREE.PointLight(0x40ebff, 60, 40, 1.5); l.position.set(0, 6, z0); scene.add(l);
  }

  // ---------------------------------------------------------------- 2. Physics Valley
  {
    const z0 = WZ(1);
    const grass = new THREE.MeshStandardMaterial({ color: 0x3fcf7a, roughness: 0.8, flatShading: true, emissive: 0x062a14 });
    const rock = new THREE.MeshStandardMaterial({ color: 0x4a4f63, roughness: 1, flatShading: true });
    const islands = [];
    for (let i = 0; i < 13; i++) {
      const g = new THREE.Group();
      const r = rr(1.2, 3.2);
      const top = new THREE.Mesh(new THREE.CylinderGeometry(r, r * 0.92, 0.5, 7), grass);
      const bottom = new THREE.Mesh(new THREE.ConeGeometry(r * 0.95, r * 2.2, 7), rock);
      bottom.rotation.x = Math.PI; bottom.position.y = -r * 1.1 - 0.25;
      g.add(top, bottom);
      if (rand() < 0.6) {
        const tree = new THREE.Mesh(new THREE.ConeGeometry(0.45, 1.4, 6), new THREE.MeshStandardMaterial({ color: 0x1f9a58, flatShading: true }));
        tree.position.set(rr(-r * 0.4, r * 0.4), 0.9, rr(-r * 0.4, r * 0.4)); g.add(tree);
      }
      let x = rr(-20, 20); if (Math.abs(x) < 4) x += Math.sign(x || 1) * 5;
      g.position.set(x, rr(-1, 8), z0 + rr(-18, 12));
      g.rotation.y = rr(0, 6);
      islands.push({ g, y: g.position.y, k: rr(0.4, 1), p: rr(0, 6) });
      scene.add(g);
    }
    const pivot = new THREE.Group(); pivot.position.set(4.5, 9, z0 - 2);
    const rod = new THREE.Mesh(new THREE.CylinderGeometry(0.03, 0.03, 6), new THREE.MeshBasicMaterial({ color: 0x7cffa0 }));
    rod.position.y = -3; pivot.add(rod);
    const bob = new THREE.Mesh(new THREE.SphereGeometry(0.5, 24, 16), new THREE.MeshStandardMaterial({ color: 0x7cffa0, emissive: 0x2aff7a, emissiveIntensity: 1.6 }));
    bob.position.y = -6; pivot.add(bob);
    scene.add(pivot);
    const planet = new THREE.Mesh(new THREE.SphereGeometry(2.2, 32, 24), new THREE.MeshStandardMaterial({ color: 0x3a6cff, emissive: 0x0a1a55, roughness: 0.5 }));
    planet.position.set(-9, 11, z0 - 14); scene.add(planet);
    const ring = new THREE.Mesh(new THREE.TorusGeometry(3.6, 0.05, 8, 120), new THREE.MeshBasicMaterial({ color: 0x9fdcff }));
    ring.rotation.x = 1.2; planet.add(ring);
    const moon = new THREE.Mesh(new THREE.SphereGeometry(0.4, 16, 12), new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0x7cffa0, emissiveIntensity: 1.2 }));
    scene.add(moon);
    updaters.push((t) => {
      islands.forEach((it) => { it.g.position.y = it.y + Math.sin(t * it.k + it.p) * 0.5; it.g.rotation.y += 0.0015; });
      pivot.rotation.z = Math.sin(t * 1.4) * 0.55;
      moon.position.set(planet.position.x + Math.cos(t * 0.6) * 5, planet.position.y + Math.sin(t * 0.6) * 1.5, planet.position.z + Math.sin(t * 0.6) * 5);
    });
    const l = new THREE.PointLight(0x7cffa0, 50, 40, 1.5); l.position.set(0, 6, z0); scene.add(l);
  }

  // ---------------------------------------------------------------- 3. Chemical Chaos
  {
    const z0 = WZ(2);
    const atomMat = {
      C: new THREE.MeshStandardMaterial({ color: 0x3b3f4a, roughness: 0.35, metalness: 0.2 }),
      O: new THREE.MeshStandardMaterial({ color: 0xff4a4a, emissive: 0x550000, roughness: 0.3 }),
      H: new THREE.MeshStandardMaterial({ color: 0xf2f6ff, roughness: 0.3 }),
      N: new THREE.MeshStandardMaterial({ color: 0x4a7dff, emissive: 0x0a1a55, roughness: 0.3 }),
    };
    const bondMat = new THREE.MeshStandardMaterial({ color: 0xffb84a, emissive: 0x5a3200, roughness: 0.4 });
    const sphere = new THREE.SphereGeometry(1, 20, 14);
    const cyl = new THREE.CylinderGeometry(0.08, 0.08, 1, 8);
    function molecule(atoms, bonds) {
      const g = new THREE.Group();
      atoms.forEach(([el, x, y, z]) => { const m = new THREE.Mesh(sphere, atomMat[el]); m.scale.setScalar(el === 'H' ? 0.28 : 0.45); m.position.set(x, y, z); g.add(m); });
      bonds.forEach(([a, b]) => {
        const A = new THREE.Vector3(...atoms[a].slice(1)), B = new THREE.Vector3(...atoms[b].slice(1));
        const m = new THREE.Mesh(cyl, bondMat);
        m.position.copy(A).add(B).multiplyScalar(0.5);
        m.scale.y = A.distanceTo(B);
        m.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), B.clone().sub(A).normalize());
        g.add(m);
      });
      return g;
    }
    const benzene = () => {
      const atoms = [], bonds = [];
      for (let i = 0; i < 6; i++) { const a = i / 6 * Math.PI * 2; atoms.push(['C', Math.cos(a) * 1.4, Math.sin(a) * 1.4, 0]); }
      for (let i = 0; i < 6; i++) { const a = i / 6 * Math.PI * 2; atoms.push(['H', Math.cos(a) * 2.5, Math.sin(a) * 2.5, 0]); bonds.push([i, (i + 1) % 6], [i, i + 6]); }
      return molecule(atoms, bonds);
    };
    const water = () => molecule([['O', 0, 0, 0], ['H', 0.95, 0.6, 0], ['H', -0.95, 0.6, 0]], [[0, 1], [0, 2]]);
    const ammonia = () => molecule([['N', 0, 0, 0], ['H', 0.95, -0.4, 0.3], ['H', -0.6, -0.4, 0.8], ['H', -0.3, -0.4, -0.95]], [[0, 1], [0, 2], [0, 3]]);
    const co2 = () => molecule([['C', 0, 0, 0], ['O', 1.3, 0, 0], ['O', -1.3, 0, 0]], [[0, 1], [0, 2]]);
    const makers = [benzene, water, ammonia, co2, water, benzene, co2, ammonia, water, benzene, ammonia, co2, water, benzene];
    const mols = makers.map((mk, i) => {
      const g = mk();
      let x = rr(-2, 16);
      g.position.set(x, rr(-1, 7), z0 + rr(-16, 6));
      g.scale.setScalar(rr(1.2, 2.2));
      scene.add(g);
      return { g, s: rr(0.2, 0.6), y: g.position.y, p: i };
    });
    const B = low ? 80 : 200;
    const bubbles = new THREE.InstancedMesh(new THREE.SphereGeometry(0.12, 10, 8), new THREE.MeshBasicMaterial({ color: 0xffb84a, transparent: true, opacity: 0.55, blending: THREE.AdditiveBlending, depthWrite: false }), B);
    const bd = []; for (let i = 0; i < B; i++) bd.push({ x: rr(-20, 20), y: rr(-3, 12), z: z0 + rr(-18, 12), s: rr(0.6, 2), k: rr(0.5, 2) });
    scene.add(bubbles);
    const m4 = new THREE.Matrix4(), v = new THREE.Vector3(), q = new THREE.Quaternion(), sc = new THREE.Vector3();
    updaters.push((t, dt) => {
      mols.forEach((m) => { m.g.rotation.y += dt * m.s; m.g.rotation.x += dt * m.s * 0.5; m.g.position.y = m.y + Math.sin(t * 0.7 + m.p) * 0.4; });
      for (let i = 0; i < B; i++) { const b = bd[i]; b.y += b.s * dt; if (b.y > 14) b.y = -3; v.set(b.x + Math.sin(t * b.k + i) * 0.3, b.y, b.z); sc.setScalar(b.k); m4.compose(v, q, sc); bubbles.setMatrixAt(i, m4); }
      bubbles.instanceMatrix.needsUpdate = true;
    });
    const l = new THREE.PointLight(0xffb84a, 60, 40, 1.5); l.position.set(0, 6, z0); scene.add(l);
  }

  // ---------------------------------------------------------------- 4. Bio-Zone
  {
    const z0 = WZ(3);
    const helix = new THREE.Group(); helix.position.set(5, 3, z0 - 4); helix.rotation.z = 0.25;
    const P = 44;
    const ball = new THREE.SphereGeometry(0.22, 14, 10);
    const sA = new THREE.InstancedMesh(ball, new THREE.MeshStandardMaterial({ color: 0xff5fa2, emissive: 0xff1f7a, emissiveIntensity: 0.9 }), P);
    const sB = new THREE.InstancedMesh(ball, new THREE.MeshStandardMaterial({ color: 0x40ebff, emissive: 0x1fb5ff, emissiveIntensity: 0.9 }), P);
    const rungs = new THREE.InstancedMesh(new THREE.CylinderGeometry(0.04, 0.04, 1, 6), new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0x8899ff, emissiveIntensity: 0.4 }), P);
    const m4 = new THREE.Matrix4(), q = new THREE.Quaternion(), up = new THREE.Vector3(0, 1, 0);
    for (let i = 0; i < P; i++) {
      const y = (i - P / 2) * 0.42, a = i * 0.42, r = 1.5;
      const A = new THREE.Vector3(Math.cos(a) * r, y, Math.sin(a) * r), Bv = new THREE.Vector3(-Math.cos(a) * r, y, -Math.sin(a) * r);
      m4.makeTranslation(A.x, A.y, A.z); sA.setMatrixAt(i, m4);
      m4.makeTranslation(Bv.x, Bv.y, Bv.z); sB.setMatrixAt(i, m4);
      q.setFromUnitVectors(up, Bv.clone().sub(A).normalize());
      m4.compose(A.clone().add(Bv).multiplyScalar(0.5), q, new THREE.Vector3(1, r * 2, 1)); rungs.setMatrixAt(i, m4);
    }
    helix.add(sA, sB, rungs);
    scene.add(helix);
    const R = low ? 24 : 50;
    const cells = new THREE.InstancedMesh(new THREE.TorusGeometry(0.5, 0.28, 12, 24), new THREE.MeshStandardMaterial({ color: 0xd8213f, emissive: 0x5a0011, roughness: 0.45 }), R);
    const cd = []; for (let i = 0; i < R; i++) cd.push({ x: rr(-22, 22), y: rr(-2, 10), z: z0 + rr(-18, 12), s: rr(1, 3), r: new THREE.Euler(rr(0, 6), rr(0, 6), 0), k: rr(0.3, 1) });
    scene.add(cells);
    const v = new THREE.Vector3(), sc = new THREE.Vector3(1, 1, 1);
    updaters.push((t, dt) => {
      helix.rotation.y += dt * 0.4;
      for (let i = 0; i < R; i++) {
        const c = cd[i]; c.x += c.s * dt; if (c.x > 24) c.x = -24;
        c.r.x += dt * c.k; c.r.y += dt * c.k * 0.6; q.setFromEuler(c.r);
        v.set(c.x, c.y + Math.sin(t * c.k + i) * 0.6, c.z); m4.compose(v, q, sc); cells.setMatrixAt(i, m4);
      }
      cells.instanceMatrix.needsUpdate = true;
    });
    const l = new THREE.PointLight(0xff5fa2, 70, 40, 1.5); l.position.set(0, 6, z0); scene.add(l);
  }

  // ---------------------------------------------------------------- 5. The Core
  {
    const z0 = WZ(4);
    const mono = new THREE.Mesh(new THREE.BoxGeometry(3, 14, 1.2, 1, 40, 1), new THREE.ShaderMaterial({
      uniforms,
      vertexShader: `uniform float uTime; varying vec2 vUv; varying vec3 vP;
        void main(){ vUv=uv; vec3 p=position; float b=floor(p.y*2.0+uTime*4.0); float g=step(0.86,fract(sin(b*91.3+floor(uTime*6.0))*473.1));
          p.x+=g*0.6*sin(b); vP=p; gl_Position=projectionMatrix*modelViewMatrix*vec4(p,1.0); }`,
      fragmentShader: `uniform float uTime; varying vec2 vUv; varying vec3 vP;
        void main(){ float scan=step(0.5,fract(vP.y*6.0-uTime*2.0)); float edge=max(step(0.96,abs(vUv.x*2.0-1.0)),step(0.985,abs(vUv.y*2.0-1.0)));
          vec3 c=mix(vec3(0.42,0.18,1.0),vec3(1.0,0.24,0.78),vUv.y)*(0.25+0.25*scan)+edge*vec3(0.8,0.6,1.0)*2.0;
          gl_FragColor=vec4(c,1.0); }`,
    }));
    mono.position.set(-5, 4, z0 - 3);
    scene.add(mono);
    const F = low ? 60 : 140;
    const frags = new THREE.InstancedMesh(new THREE.OctahedronGeometry(0.25), new THREE.MeshStandardMaterial({ color: 0xb36bff, emissive: 0x6a2bff, emissiveIntensity: 1.4, flatShading: true }), F);
    const fd = []; for (let i = 0; i < F; i++) fd.push({ r: rr(3, 9), a: rr(0, 6.28), y: rr(-2, 12), s: rr(0.2, 0.8) });
    scene.add(frags);
    const beam = new THREE.Mesh(new THREE.CylinderGeometry(0.35, 0.35, 60, 16, 1, true), new THREE.MeshBasicMaterial({ color: 0xb36bff, transparent: true, opacity: 0.35, blending: THREE.AdditiveBlending, depthWrite: false }));
    beam.position.set(-5, 20, z0 - 3); scene.add(beam);
    const m4 = new THREE.Matrix4(), v = new THREE.Vector3(), q = new THREE.Quaternion(), e = new THREE.Euler(), sc = new THREE.Vector3(1, 1, 1);
    updaters.push((t, dt) => {
      mono.rotation.y = Math.sin(t * 0.3) * 0.4;
      for (let i = 0; i < F; i++) { const f = fd[i]; f.a += dt * f.s; v.set(mono.position.x + Math.cos(f.a) * f.r, f.y + Math.sin(t + i) * 0.3, mono.position.z + Math.sin(f.a) * f.r); e.set(t * f.s * 3, t, 0); q.setFromEuler(e); m4.compose(v, q, sc); frags.setMatrixAt(i, m4); }
      frags.instanceMatrix.needsUpdate = true;
      beam.material.opacity = 0.25 + 0.15 * Math.sin(t * 5.0);
    });
    const l = new THREE.PointLight(0xb36bff, 80, 40, 1.5); l.position.set(-3, 6, z0); scene.add(l);
  }

  // ---------------------------------------------------------------- 6. Exam Hall
  {
    const z0 = WZ(5);
    const deskMat = new THREE.MeshStandardMaterial({ color: 0x6b4a2f, roughness: 0.7 });
    const legMat = new THREE.MeshStandardMaterial({ color: 0x2a2d36, metalness: 0.6, roughness: 0.4 });
    const rows = 5, cols = 8;
    const tops = new THREE.InstancedMesh(new THREE.BoxGeometry(1.6, 0.1, 1), deskMat, rows * cols);
    const legs = new THREE.InstancedMesh(new THREE.BoxGeometry(0.08, 1.2, 0.08), legMat, rows * cols * 4);
    const m4 = new THREE.Matrix4();
    let k = 0, li = 0;
    for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) {
      const x = (c - (cols - 1) / 2) * 3.2, z = z0 + 6 - r * 3.4, y = -1.8;
      m4.makeTranslation(x, y, z); tops.setMatrixAt(k++, m4);
      for (const [dx, dz] of [[-0.7, -0.4], [0.7, -0.4], [-0.7, 0.4], [0.7, 0.4]]) { m4.makeTranslation(x + dx, y - 0.6, z + dz); legs.setMatrixAt(li++, m4); }
    }
    scene.add(tops, legs);
    const PN = low ? 40 : 90;
    const papers = new THREE.InstancedMesh(new THREE.PlaneGeometry(0.6, 0.85), new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0x666677, side: THREE.DoubleSide }), PN);
    const pd = []; for (let i = 0; i < PN; i++) pd.push({ x: rr(-14, 14), y: rr(0, 10), z: z0 + rr(-12, 8), k: rr(0.5, 1.5), e: new THREE.Euler(rr(0, 6), rr(0, 6), rr(0, 6)) });
    scene.add(papers);
    const clock = new THREE.Group(); clock.position.set(0, 9, z0 - 12);
    clock.add(new THREE.Mesh(new THREE.TorusGeometry(3, 0.12, 12, 80), new THREE.MeshBasicMaterial({ color: 0xff3b4e })));
    for (let i = 0; i < 12; i++) { const tick = new THREE.Mesh(new THREE.BoxGeometry(0.08, 0.5, 0.05), new THREE.MeshBasicMaterial({ color: 0xffd0d4 })); const a = i / 12 * Math.PI * 2; tick.position.set(Math.sin(a) * 2.5, Math.cos(a) * 2.5, 0); tick.rotation.z = -a; clock.add(tick); }
    const hand = new THREE.Mesh(new THREE.BoxGeometry(0.1, 2.2, 0.05), new THREE.MeshBasicMaterial({ color: 0xffffff }));
    hand.geometry.translate(0, 1.1, 0); clock.add(hand);
    scene.add(clock);
    const eye = new THREE.Mesh(new THREE.SphereGeometry(0.9, 32, 20), new THREE.MeshBasicMaterial({ color: 0xff2030 }));
    eye.position.set(0, 9, z0 - 11.8); scene.add(eye);
    const q = new THREE.Quaternion(), v = new THREE.Vector3(), sc = new THREE.Vector3(1, 1, 1);
    updaters.push((t, dt) => {
      hand.rotation.z = -Math.floor(t) * (Math.PI * 2 / 60);
      eye.scale.setScalar(0.85 + 0.15 * Math.sin(t * 2.2));
      for (let i = 0; i < PN; i++) {
        const p = pd[i]; p.y -= dt * 0.3 * p.k; if (p.y < -1.5) p.y = 11;
        p.e.x += dt * p.k; p.e.z += dt * p.k * 0.7; q.setFromEuler(p.e);
        v.set(p.x + Math.sin(t * p.k + i) * 0.8, p.y, p.z); m4.compose(v, q, sc); papers.setMatrixAt(i, m4);
      }
      papers.instanceMatrix.needsUpdate = true;
    });
    const l = new THREE.PointLight(0xff3b4e, 80, 40, 1.5); l.position.set(0, 7, z0); scene.add(l);
    const lamp = new THREE.PointLight(0xfff1d6, 40, 30, 1.5); lamp.position.set(0, 8, z0 + 4); scene.add(lamp);
  }

  // ---------------------------------------------------------------- camera stops (one per page section, data-cam)
  const V = (x, y, z) => new THREE.Vector3(x, y, z);
  const worldCam = (i, side) => ({ pos: V(-side * 3.5, 2.6, WZ(i) + 15), look: V(side * 3.5, 1.5, WZ(i) - 4) });
  const cams = [
    { pos: V(0, 0.3, 8), look: V(0.6, 0.3, 0) },          // 0 hero
    { pos: V(-3.5, 3, 11), look: V(2.2, 0, -4) },        // 1 story
    worldCam(0, 1), worldCam(1, -1), worldCam(2, 1), worldCam(3, -1), worldCam(4, 1), worldCam(5, -1),
    { pos: V(0, 9, END_Z + 26), look: V(0, -1, END_Z) },   // 8 features
    { pos: V(5, 2.5, END_Z + 13), look: V(-4.5, 0.5, END_Z) },  // 9 gallery
    { pos: V(-7, 1.5, END_Z + 12), look: V(-6, 0.5, END_Z) }, // 10 verify
    { pos: V(-3.4, 0.6, END_Z + 10.5), look: V(-6.4, 0.6, END_Z) }, // 11 install
    { pos: V(0, 5, END_Z + 18), look: V(0, 0, END_Z) },    // 12 credits
  ];
  const fogColors = [0x05060d, 0x070816, 0x040b19, 0x03120b, 0x150b02, 0x14030c, 0x0b0418, 0x150205, 0x05060d, 0x05060d, 0x05060d, 0x06071a, 0x05060d].map((c) => new THREE.Color(c));

  let camT = 0, pointerX = 0, pointerY = 0;
  const curPos = cams[0].pos.clone(), curLook = cams[0].look.clone();
  const tgtPos = new THREE.Vector3(), tgtLook = new THREE.Vector3(), fog = new THREE.Color();

  function resize() {
    const w = window.innerWidth, h = window.innerHeight;
    renderer.setSize(w, h, false);
    composer.setSize(w, h);
    camera.aspect = w / h;
    camera.fov = w < 760 ? 68 : 55;
    camera.updateProjectionMatrix();
  }
  window.addEventListener('resize', resize);
  resize();

  const clock = new THREE.Clock();
  let burstV = 0, chargeV = 0, chargeTarget = 0, running = true;
  document.addEventListener('visibilitychange', () => { running = !document.hidden; if (running) { clock.getDelta(); loop(); } });

  function loop() {
    if (!running) return;
    requestAnimationFrame(loop);
    const dt = Math.min(clock.getDelta(), 0.05);
    const t = (uniforms.uTime.value += dt * (reduced ? 0.3 : 1));
    chargeV += (chargeTarget - chargeV) * (1 - Math.exp(-dt * 10));
    burstV *= Math.exp(-dt * 2.2);
    uniforms.uCharge.value = chargeV;
    uniforms.uBurst.value = burstV;

    const i = Math.max(0, Math.min(cams.length - 2, Math.floor(camT)));
    const f = smooth(Math.max(0, Math.min(1, camT - i)));
    tgtPos.lerpVectors(cams[i].pos, cams[i + 1].pos, f);
    tgtLook.lerpVectors(cams[i].look, cams[i + 1].look, f);
    tgtPos.x += pointerX * 0.6; tgtPos.y += -pointerY * 0.35;
    const k = 1 - Math.exp(-dt * (reduced ? 2 : 3.2));
    curPos.lerp(tgtPos, k); curLook.lerp(tgtLook, k);
    if (burstV > 0.02) curPos.add(new THREE.Vector3(rr(-1, 1), rr(-1, 1), 0).multiplyScalar(burstV * 0.12));
    camera.position.copy(curPos);
    camera.lookAt(curLook);
    fog.copy(fogColors[i]).lerp(fogColors[i + 1], f);
    scene.fog.color.copy(fog); scene.background.copy(fog);
    for (const u of updaters) u(t, reduced ? dt * 0.3 : dt);
    composer.render(dt);
  }
  loop();

  return {
    setCamera(v) { camT = Math.max(0, Math.min(cams.length - 1, v)); },
    setPointer(x, y) { pointerX = x; pointerY = y; },
    charge(p) { chargeTarget = p; },
    burst() { burstV = 1; chargeTarget = 0; },
    stops: cams.length,
  };
}
