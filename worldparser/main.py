import amulet
import json
import numpy as np
import base64

from amulet.api.errors import ChunkDoesNotExist

def main():
    x_offset = -6
    z_offset = -8

    level = amulet.load_format("worlds/Lighting")
    level.open()
    for y in range(0, 4):
        for z in range(-3, 3):
            for x in range(-2, 2):
                result = export_chunk(level, x, y, z, x_offset, z_offset)
                with open(f"out/{x}_{y}_{z}.json", "wt") as f:
                    f.write(result)
    level.close()

def export_chunk(level: amulet.api.wrapper.WorldFormatWrapper, cx, cy, cz, offset_x, offset_z) -> str:
    palette = Palette()
    export_data = np.zeros((32, 32, 32), np.int16)

    for cx_offset in range(0, 2):
        for cz_offset in range(0, 2):
            try:
                chunk = level.load_chunk(2*(cx+offset_x)+cx_offset, 2*(cz+offset_z)+cz_offset, "minecraft:overworld")

                palette.add_amulet(chunk.block_palette)

                for x in range(0, 16):
                    for z in range(0, 16):
                        for y in range(0, 32):
                            export_data[y, z + cz_offset * 16, x + cx_offset * 16] = palette.get(chunk.block_palette[chunk.blocks[x, y + 32 * cy, z]].base_name)
                print(f"EXPORTED chunk({cx},{cy},{cz})")
            except ChunkDoesNotExist:
                print(f"SKIPPED chunk({cx},{cy},{cz}) as it does not exist")
    
    result = {
        "x": cx,
        "y": cy,
        "z": cz,
        "block_palette": palette.dict,
        "blocks": export_data.flatten().tolist()
    }
    return json.dumps(result)

class Palette:
    def __init__(self):
        self.dict = {"air": 0}
        self.next_id = 1

    def add_amulet(self, palette: amulet.api.registry.BlockManager):
        for b in palette.blocks:
            if b.base_name not in self.dict:
                self.dict[b.base_name] = self.next_id
                self.next_id += 1

    def get(self, block: str) -> int:
        return self.dict[block]

if __name__ == "__main__":
    main()