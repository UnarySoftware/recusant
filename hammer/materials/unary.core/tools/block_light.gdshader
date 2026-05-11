shader_type spatial;
render_mode unshaded, fog_disabled, ambient_light_disabled;

void fragment()
{
    // This got to be a black color, otherwise we get an unnecessary GI contributtions
    ALBEDO = vec3(0.0, 0.0, 0.0);
}
