// Rain Droplets on Glass – Godot 4 Spatial Shader
// Ported from "Heartfelt" by Martijn Steinrucken (BigWings) - 2017
// Original: https://www.shadertoy.com/view/ltffzl
// License: CC BY-NC-SA 3.0

shader_type spatial;
render_mode unshaded, depth_draw_never, cull_disabled;

uniform sampler2D screen_texture   : hint_screen_texture, filter_linear_mipmap;
uniform float rain_amount : hint_range(0.0, 1.0) = 0.7;
uniform float rain_speed  : hint_range(0.1, 4.0) = 1.0;
uniform float max_blur    : hint_range(0.0, 8.0) = 5.0;
uniform float min_blur    : hint_range(0.0, 4.0) = 2.0;
uniform float scale_x    : hint_range(0.0, 10.0) = 1.0;
uniform float scale_y    : hint_range(0.0, 10.0) = 1.0;

float N(float t) 
{
    return fract(sin(t * 12345.564) * 7658.76);
}

vec3 N13(float p) 
{
    vec3 p3 = fract(vec3(p) * vec3(0.1031, 0.11369, 0.13787));
    p3 += dot(p3, p3.yzx + 19.19);
    return fract(vec3(
        (p3.x + p3.y) * p3.z,
        (p3.x + p3.z) * p3.y,
        (p3.y + p3.z) * p3.x
    ));
}

float Saw(float b, float t) 
{
    return smoothstep(0.0, b, t) * smoothstep(1.0, b, t);
}

vec2 DropLayer2(vec2 uv, float t) 
{
    vec2 UV = uv;

    uv.y += t * 0.75;
    vec2 a    = vec2(6.0, 1.0);
    vec2 grid = a * 2.0;
    vec2 id   = floor(uv * grid);

    float colShift = N(id.x);
    uv.y += colShift;

    id       = floor(uv * grid);
    vec3  n  = N13(id.x * 35.2 + id.y * 2376.1);
    vec2  st = fract(uv * grid) - vec2(0.5, 0.0);

    float x = n.x - 0.5;

    float y      = UV.y * 20.0;
    float wiggle = sin(y + sin(y));
    x += wiggle * (0.5 - abs(x)) * (n.z - 0.5);
    x *= 0.7;

    float ti = fract(t + n.z);
    y = (Saw(0.85, ti) - 0.5) * 0.9 + 0.5;

    vec2  p        = vec2(x, y);
    float d        = length((st - p) * a.yx);
    float mainDrop = smoothstep(0.4, 0.0, d);

    float r         = sqrt(smoothstep(1.0, y, st.y));
    float cd        = abs(st.x - x);
    float trail     = smoothstep(0.23 * r, 0.15 * r * r, cd);
    float trailFront = smoothstep(-0.02, 0.02, st.y - y);
    trail *= trailFront * r * r;

    y = UV.y;
    float trail2   = smoothstep(0.2 * r, 0.0, cd);
    float droplets = max(0.0, (sin(y * (1.0 - y) * 120.0) - st.y)) * trail2 * trailFront * n.z;
    y  = fract(y * 10.0) + (st.y - 0.5);
    float dd = length(st - vec2(x, y));
    droplets = smoothstep(0.3, 0.0, dd);

    float m = mainDrop + droplets * r * trailFront;
    return vec2(m, trail);
}

float StaticDrops(vec2 uv, float t) 
{
    uv *= 40.0;
    vec2  id  = floor(uv);
    uv = fract(uv) - 0.5;
    vec3  n   = N13(id.x * 107.45 + id.y * 3543.654);
    vec2  p   = (n.xy - 0.5) * 0.7;
    float d   = length(uv - p);
    float fade = Saw(0.025, fract(t + n.z));
    return smoothstep(0.3, 0.0, d) * fract(n.z * 10.0) * fade;
}

vec2 Drops(vec2 uv, float t, float l0, float l1, float l2)
{
    float s  = StaticDrops(uv, t) * l0;
    vec2  m1 = DropLayer2(uv, t) * l1;
    vec2  m2 = DropLayer2(uv * 1.85, t) * l2;

    float c = s + m1.x + m2.x;
    c = smoothstep(0.3, 1.0, c);
    return vec2(c, max(m1.y * l0, m2.y * l1));
}

void fragment() 
{
    vec2 uv = SCREEN_UV;

    vec2 rain_uv = UV;
	rain_uv.r *= scale_x;
	rain_uv.g *= scale_y;
	
	vec3 col_deb;
	col_deb.rg = rain_uv;
	col_deb.b = 1.0;
	
    float t = -TIME * rain_speed * 0.2;

    // Derive layer weights from rain_amount uniform
    float staticDrops = smoothstep(-0.5, 1.0, rain_amount) * 2.0;
    float layer1      = smoothstep(0.25, 0.75, rain_amount);
    float layer2      = smoothstep(0.0,  0.5,  rain_amount);

    // Compute drops
    vec2 c = Drops(rain_uv, t, staticDrops, layer1, layer2);

    // Normals (expensive path – gradient of drop density)
    vec2 e  = vec2(0.001, 0.0);
    float cx = Drops(rain_uv + e,     t, staticDrops, layer1, layer2).x;
    float cy = Drops(rain_uv + e.yx,  t, staticDrops, layer1, layer2).x;
    vec2  n  = vec2(cx - c.x, cy - c.x);

    // Blur amount: clear under drops, foggy everywhere else
    float focus = mix(max_blur - c.y, min_blur, smoothstep(0.1, 0.2, c.x));

    // Sample the screen with mip-based blur for the fog/frost effect
    vec3 col = textureLod(screen_texture, uv + n, focus).rgb;

    ALBEDO = col;
    ALPHA  = 1.0;
}
