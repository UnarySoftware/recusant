shader_type spatial;
render_mode unshaded, specular_disabled, fog_disabled;

instance uniform vec4 color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform sampler2D grid_texture;

void fragment()
{
	vec4 data = texture(grid_texture, UV * (color.a * 255.0f));
	ALBEDO = data.rgb * color.rgb;
	ALPHA = data.a;
}
