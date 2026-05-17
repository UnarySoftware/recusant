// https://godotshaders.com/shader/celestial-shader-2/

shader_type sky;

uniform sampler2D sky_texture;
uniform float power = 0.2;
uniform float scale_a = 0.5;
uniform float scale_b = 0.3;
uniform float speed = 0.2;
uniform vec2 speed_a = vec2(0.1);
uniform vec2 speed_b = vec2(0.1);
uniform float density : hint_range(0.0, 2.0) = 0.9;
uniform float grayness : hint_range(0.0, 1.0) = 0.2;
uniform float cloud_light = 1.0;
uniform float cloud_distortion = 0.2;

uniform vec3 sky_color_a0 : source_color = vec3(0.5, 0.8, 1.0);
uniform vec3 sky_color_b0 : source_color = vec3(0.3, 0.5, 1.0);
uniform vec3 sky_color_a1 : source_color = vec3(0.125, 0.2, 0.25);
uniform vec3 sky_color_b1 : source_color = vec3(0.075, 0.125, 0.25);

uniform vec3 ground_color_a0 : source_color = vec3(1.0, 0.8, 0.5);
uniform vec3 ground_color_b0 : source_color = vec3(1.0, 0.5, 0.3);
uniform vec3 ground_color_a1 : source_color = vec3(0.25, 0.2, 0.125);
uniform vec3 ground_color_b1 : source_color = vec3(0.25, 0.125, 0.075);

uniform vec3 sun_color_a : source_color = vec3(1.0, 0.98, 0.025);
uniform vec3 sun_color_b : source_color = vec3(1.0, 0.5, 0.0);
uniform float sun_size = 0.995; 
uniform float sun_softness = 0.002;
uniform vec2 sunset_range = vec2(-0.1, 0.3);

float to_grayscale(vec3 color) {
	return dot(color, vec3(0.299, 0.587, 0.114));
}

vec3 do_ceil(vec3 eye, vec3 sun) {
	vec3 mix_0 = mix(sky_color_a0, sky_color_b0, eye.y);
	vec3 mix_1 = mix(sky_color_a1, sky_color_b1, eye.y);
	return mix(mix_1, mix_0, smoothstep(sunset_range.x, sunset_range.y, sun.y));
}

vec3 do_floor(vec3 eye, vec3 sun) {
	vec3 mix_0 = mix(ground_color_a0, ground_color_b0, -eye.y);
	vec3 mix_1 = mix(ground_color_a1, ground_color_b1, -eye.y);
	return mix(mix_1, mix_0, smoothstep(sunset_range.x, sunset_range.y, sun.y));
}

void sky() {
	float fade = pow(abs(EYEDIR.y), power);
	vec2 uv_a = (EYEDIR.xz / EYEDIR.y) * scale_a + TIME * speed_a * speed;
	vec2 uv_b = (EYEDIR.xz / EYEDIR.y) * scale_b + TIME * speed_b * speed;
	uv_b += texture(sky_texture, uv_a).rg * cloud_distortion;
	vec3 clouds_a = texture(sky_texture, uv_a).rgb * fade;
	vec3 clouds_b = texture(sky_texture, uv_b).rgb * fade;
	vec3 clouds = smoothstep(density, 2.0, clouds_a + clouds_b);
	vec3 sky_back = EYEDIR.y > 0.0 ? do_ceil(EYEDIR, LIGHT0_DIRECTION) : vec3(0.0);
	
	// sky + clouds
	vec3 sky_with_clouds = sky_back + clouds * cloud_light;
	
	// the sun is easy to calculate thanks to the dot product between EYEDIR and LIGHT0_DIRECTION
	float sun_dot = max(0.0, dot(EYEDIR, LIGHT0_DIRECTION)); 
	float sun = smoothstep(sun_size, sun_size + sun_softness, sun_dot);
	
	// draw the sun + sky
	COLOR = vec3(0.0);
	COLOR = EYEDIR.y > 0.0 ? mix(sky_with_clouds, vec3(to_grayscale(sky_with_clouds)), grayness) : COLOR;
	COLOR += sun * mix(sun_color_b, sun_color_a, smoothstep(sunset_range.x, sunset_range.y, LIGHT0_DIRECTION.y));
	
	// draw the ground over the sky
	COLOR = EYEDIR.y < 0.0 ? do_floor(EYEDIR, LIGHT0_DIRECTION) : COLOR; 	
}