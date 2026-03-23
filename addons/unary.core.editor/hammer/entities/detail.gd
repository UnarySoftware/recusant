@tool
class_name Detail extends VMFEntityNode

func _entity_setup(_entity: VMFEntity):
	var mesh = get_mesh();
	mesh.lightmap_unwrap(global_transform, config.import.lightmap_texel_size);

	if !mesh or mesh.get_surface_count() == 0:
		queue_free();
		return;

	$MeshInstance3D.set_mesh(mesh);
	
	var geometry_instance : GeometryInstance3D = $MeshInstance3D
	geometry_instance.gi_mode = _entity.data.get("gimode") as GeometryInstance3D.GIMode
	
	var collisions = _entity.data.get("collision") as bool
	
	if collisions:
		$MeshInstance3D/StaticBody3D/CollisionShape3D.shape = mesh.create_trimesh_shape();
		var surface_prop_name = _entity.data.get("surfacepropname")
		$MeshInstance3D/StaticBody3D.set_meta("surface_prop", surface_prop_name)
	else:
		$MeshInstance3D/StaticBody3D.queue_free()
	
	self.set_script(null)
