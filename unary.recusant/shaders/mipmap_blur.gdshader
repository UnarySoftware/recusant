shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap, repeat_disable;

uniform float blur_lod     : hint_range(0.0, 8.0, 0.1) = 3.5;
uniform float jitter       : hint_range(0.0, 1.0, 0.01) = 0.3;
uniform int   jitter_taps  : hint_range(1, 8, 1)   = 4;
uniform vec4  tint_color   : source_color = vec4(1.0, 1.0, 1.0, 0.1);
uniform float saturation   : hint_range(0.0, 2.0, 0.01) = 1.2;
uniform float vignette_str : hint_range(0.0, 1.0, 0.01) = 0.15;

float rand(vec2 co)
{
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

vec3 apply_saturation(vec3 color, float sat)
{
    float luma = dot(color, vec3(0.2126, 0.7152, 0.0722));
    return mix(vec3(luma), color, sat);
}

void fragment() 
{
    vec2 px = vec2(1.0) / vec2(textureSize(SCREEN_TEXTURE, 0));

    vec3 accum = vec3(0.0);
    float seed = rand(SCREEN_UV);

    for (int i = 0; i < jitter_taps; i++) 
	{
        float angle  = (float(i) + seed) * 2.3999632;
        vec2 offset = vec2(cos(angle), sin(angle)) * jitter * px * pow(2.0, blur_lod);
        accum += textureLod(SCREEN_TEXTURE, SCREEN_UV + offset, blur_lod).rgb;
    }
    accum /= float(jitter_taps);

    accum = apply_saturation(accum, saturation);

    vec2  centered = SCREEN_UV * 2.0 - 1.0;
    float vignette = 1.0 - vignette_str * dot(centered, centered);
    accum *= vignette;

    ALBEDO = mix(accum, tint_color.rgb, tint_color.a);
    ALPHA  = 1.0;
}