import { Link } from "react-router-dom";
import DropdownMenu from "./DropdownMenu";
import "../styles/Navbar.css";
import { LuLayoutDashboard } from "react-icons/lu";
import { FiBox, FiUsers, FiShoppingCart } from "react-icons/fi";
function Navbar() {
  return (

    <nav className="nav-container">
      <Link to="/"><LuLayoutDashboard size={18}/> Suvestinė</Link>

      <DropdownMenu
        title="Naudotojai"
        icon={<FiUsers size={18}/>}
        items={[{ label: "Naudotojų sąrašas", path: "/usersList" },
          { label: "Kurti naudotoją", path: "/userAdd" },
        ]} />

      <DropdownMenu
        title="Užsakymai"
        icon={<FiShoppingCart size={18}/>}
        items={[{ label: "Užsakymų sąrašas", path: "/orderList" },
          { label: "Kurti užsakymą", path: "/orderAdd" },

        ]} />

      <DropdownMenu
        title="Prekės"
        icon={<FiBox size={18} />}
        items={[
          { label: "Prekių sąrašas", path: "/productList" },
          { label: "Kurti prekę", path: "/productAdd" },

        ]} />
    </nav>

  );
}

export default Navbar;
