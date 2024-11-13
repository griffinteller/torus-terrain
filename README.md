# Torus Terrain

Fractal gradient noise on a torus.

(Doughnut worlds)

<img height="300" alt="Screenshot 2024-11-13 at 4 04 00 PM" src="https://github.com/user-attachments/assets/88f283aa-ddfc-48a0-a08d-20059bd71070">
<img height="300" alt="Screenshot 2024-11-13 at 4 03 03 PM" src="https://github.com/user-attachments/assets/dcff4ee0-a7cc-44de-b927-1f2ceca2f4f1">

---

### Approach

Generating gradient noise on manifolds with varying curvature seems like it should be as simple as finding a reasonable simplical complex and running simplex noise. I found the results of that approach visually distressing. This implementation takes a different approach, by doing away with connectivity information all together, and generating gradient noise per point instead of per simplex.

For a particular layer of noise, $n$ uniformly spaced points $\\{p_i \in R^3\\}$ around the torus are chosen, each mapping to a uniformly random unit gradient, $g_i \in \mathbb R^2$. Each point $p_i$ contributes the plane lying on $p_i$ with gradient $g_i$, but with influence that falls off smoothly towards 0. In particular, each point has a "radius of influence" $r$ equal to the mean distance between closest points. More specifically, the contribution to the surface for each point $f_i : \mathbb R^3 \to \mathbb R$ is given by
```math
  f_i(p) = g_i^T(p - p_i) \cdot \text{smootherstep}(0, 1, 1-\frac{\|p - p_i\|}{r})
```
The accumulation of contributions $p \mapsto \sum_{i=1}^n f_i(p)$ can be efficiently computed through off-screen rendering. Since the contribution from any sample point is contained solely within a quad of side length $2r$, you only need to compute the contribution within that quad. So, it suffices to render these quads to the heightmap (making sure to map each quad vertex to the correct uv in the vertex shader) and calculate the height contribution in the fragment shader, finally blending with `add`.
