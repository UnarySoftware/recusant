shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap, repeat_disable;
uniform sampler2D depth_texture : hint_depth_texture,  filter_nearest, repeat_disable;

uniform sampler2D glass_normal : hint_normal, filter_linear;

uniform float blur_amount : hint_range(0.0, 8.0, 0.1)  = 3.5;
uniform float distortion : hint_range(0.0, 0.05, 0.001)= 0.018;
uniform vec4 glass_tint : source_color = vec4(0.85, 0.92, 1.0, 0.15);
uniform float fresnel_pow : hint_range(1.0, 8.0, 0.1)  = 3.0;
uniform float fresnel_str : hint_range(0.0, 1.0, 0.01) = 0.35;
uniform vec3 reflect_color : source_color = vec3(0.9, 0.95, 1.0);
uniform float saturation : hint_range(0.0, 2.0, 0.01) = 1.25;
uniform float chromatic_ab : hint_range(0.0, 0.01, 0.0001) = 0.003;

vec3 apply_saturation(vec3 col, float sat) 
{
    float luma = dot(col, vec3(0.2126, 0.7152, 0.0722));
    return mix(vec3(luma), col, sat);
}

void fragment() {
    vec2 n_sample = texture(glass_normal, UV).xy * 2.0 - 1.0;

    vec2 distorted_uv = SCREEN_UV + n_sample * distortion;

    vec3 blurred;
    blurred.r = textureLod(SCREEN_TEXTURE, distorted_uv + vec2( chromatic_ab, 0.0), blur_amount).r;
    blurred.g = textureLod(SCREEN_TEXTURE, distorted_uv, blur_amount).g;
    blurred.b = textureLod(SCREEN_TEXTURE, distorted_uv - vec2( chromatic_ab, 0.0), blur_amount).b;

    blurred = apply_saturation(blurred, saturation);

    vec3 view_dir = normalize(-VIEW);
    vec3 surf_norm = NORMAL;
    float fresnel = pow(1.0 - max(dot(surf_norm, view_dir), 0.0), fresnel_pow);
    vec3 rim = reflect_color * fresnel * fresnel_str;

    vec3 glass = mix(blurred, glass_tint.rgb, glass_tint.a);
    glass += rim;

    vec2 cen = abs(UV - 0.5) * 2.0;
    float edge = 1.0 - smoothstep(0.85, 1.0, max(cen.x, cen.y));
    glass = mix(blurred, glass, edge);

    ALBEDO = clamp(glass, 0.0, 1.0);
    ALPHA  = 1.0;
}
