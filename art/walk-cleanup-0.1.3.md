# Walk cleanup 0.1.3

Tool: built-in OpenAI image generation, edit mode, transparent background.
Inputs/outputs: PhoebeFarmer/assets/are/walk.png and PhoebeFarmer/assets/dumpling/walk.png.
Prompt brief used for both: preserve the existing Phoebe costume and design; create a transparent 4x4 walk sheet with front/right/back/left rows; duplicate the same head, hat, hair, face and costume shapes within each row; alternate shoes/lower legs, with only small hand motion; keep first/third neutral poses; use consistent pixel grid, contiguous flat colors, clean thin outlines, no floating pixels or changing highlights.

Generated frames still vary slightly. The renderer therefore reuses the neutral upper 34 rows of each 40x40 directional frame and animates the bottom six rows. This deliberately reduces upper-body motion to prevent outline flicker. Other action sheets are unchanged.
