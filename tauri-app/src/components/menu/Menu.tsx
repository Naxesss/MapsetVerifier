import {NavLink} from "react-router-dom";
import {JSX} from "react";
import {
  RiBook3Fill,
  RiCameraFill,
  RiCheckboxFill,
  RiFileTextFill,
  RiSettings2Fill
} from "react-icons/ri";
import "./Menu.scss";

function Menu() {
  return (
    <nav className="navbar">
      <div className="navbar-menu-container">
        <div className="navbar-menu">
          <NavLinkItem icon={<RiBook3Fill />} label={"Documentation"} to={"/documentation"} />
          <NavLinkItem icon={<RiCheckboxFill />} label={"Checks"} to={"/"} />
          <NavLinkItem icon={<RiCameraFill />} label={"Snapshots"} to={"/snapshots"} />
          <NavLinkItem icon={<RiFileTextFill />} label={"Overview"} to={"/overview"} />
          <NavLinkItem icon={<RiSettings2Fill />} to={"/settings"} />
        </div>
      </div>
      <hr />
    </nav>
  );
}

interface NavLinkItemProps {
  icon: JSX.Element
  label?: string
  to: string
}

function NavLinkItem(props: NavLinkItemProps): JSX.Element {
  return (
    <NavLink to={props.to} className={({isActive}) => isActive ? "active" : ""}>
      <div className="nav-item">
        {props.icon}
        {props.label && (
            <div className="nav-item-label">
              {props.label}
            </div>
          )
        }
      </div>
    </NavLink>
  );
}

export default Menu;